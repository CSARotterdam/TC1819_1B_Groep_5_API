using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Logging;
using API.Requests;
using System.Reflection;
using MySQLWrapper.Data;
using static API.Requests.RequestMethodAttributes;

namespace API.Threads {
	class RequestWorker {

		//RequestWorker threads takes and processes incoming requests from the requestQueue (which are added to the queue by the Listener thread.
		public static void main(Logger log, BlockingCollection<HttpListenerContext> requestQueue) {
			MethodInfo[] methods = typeof(RequestMethods).GetMethods();

			log.Info("Thread " + Thread.CurrentThread.Name + " now running.");
			while (true) {
				// Wait for request.
				HttpListenerContext context = requestQueue.Take();
				HttpListenerRequest request = context.Request;
				log.Fine("Processing request.");

				// If API error state is true, cancel the request (because it'll fail anyway) and send an error.
				if (Program.ErrorCode != 0){
					sendMessage(context, "Code"+Program.ErrorCode.ToString(), HttpStatusCode.InternalServerError);
					continue;
				}
				// Check if content type is application/json. Send a HTTP 415 UnsupportedMediaType if it isn't.
				if (request.ContentType != "application/json") {
					log.Error("Request has invalid content type. Sending error response and ignoring!");
					sendHTMLError(context, "If at first you don't succeed, fail 5 more times.", HttpStatusCode.UnsupportedMediaType);
					continue;
				}
				// Check if request has body data. Send a 400 BadRequest if it doesn't.
				if (!request.HasEntityBody) {
					log.Error("Request has no body data. Sending error response and ignoring!");
					sendMessage(context, "Empty body data", HttpStatusCode.BadRequest);
                    continue;
				}

				//Convert request data to JObject
				System.IO.Stream body = request.InputStream;
				System.Text.Encoding encoding = request.ContentEncoding;
				System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
				JObject requestContent = JObject.Parse(reader.ReadToEnd());

                //Create base response JObject
                bool sendResponse = false;
                JObject responseJson = new JObject() {
                    {"requestData", new JObject() }
                };

                // Select request handler
                HttpStatusCode statusCode = HttpStatusCode.OK;
				MethodInfo requestMethod = null;
				foreach (MethodInfo method in methods) {
					if (method.Name == requestContent["requestType"].ToString()){
						requestMethod = method;
                        break;
					}
				}
                
                //If no request handler was found, send an error response
                if (requestMethod == null) {
                    log.Error("Request has invalid requestType value: " + requestContent["requestType"].ToString());
                    statusCode = HttpStatusCode.BadRequest;
                    responseJson["requestData"] = Templates.InvalidRequestType;
                    sendResponse = true;
                }

                //If the request handler requires the user token or permission level to be verified first, do that now.
                bool verifyToken = false;
                bool verifyPermission = false;
                User user = null;
                long token = 0;
                string username;

                //If either token or permission verification is required, get the username, token and user object.
                if (!sendResponse) {
                    verifyToken = requestMethod.GetCustomAttribute<skipTokenVerification>() == null;
                    verifyPermission = requestMethod.GetCustomAttribute<verifyPermission>() != null;

                    if (verifyToken || verifyPermission) {
                        requestContent.TryGetValue("username", out JToken usernameValue);
                        requestContent.TryGetValue("token", out JToken tokenValue);

                        if (usernameValue == null || tokenValue == null || usernameValue.Type != JTokenType.String || tokenValue.Type != JTokenType.Integer) {
							responseJson["requestData"] = Templates.MissingArguments("username, token");
                            sendResponse = true;
                        } else {
                            token = tokenValue.ToObject<long>();
                            username = usernameValue.ToObject<string>();
                            user = RequestMethods.getUser(username);
                            if (user == null) {
                                responseJson["requestData"] = Templates.InvalidLogin;
                                sendResponse = true;
                            }
                        }
                    }
                }

                //Check the user's permission level, unless the requesttype doesn't require it.
                if(!sendResponse && verifyToken) {
                    System.DateTime tokenDT = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                    tokenDT = tokenDT.AddSeconds(token).ToLocalTime();
                    bool validToken = !((DateTime.Today - tokenDT).TotalSeconds > (double)Program.Settings["authenticationSettings"]["expiration"]);

                    if (!(validToken && token == user.Token)) {
                        responseJson["requestData"] = Templates.ExpiredToken;
                        sendResponse = true;
                    }
                }
                
                //Check the user's permission level if the requesttype requires it.
                if(!sendResponse && verifyPermission) {
                    if(user.Permission < requestMethod.GetCustomAttribute<verifyPermission>().permission) {
                        responseJson["requestData"] = Templates.AccessDenied;
                        sendResponse = true;
                    }
                }
                
                //Check if the database is available, just in case.
                if (!API.Threads.DatabaseMaintainer.Ping()) {
                    responseJson["requestData"] = Templates.ServerError("DatabaseConnectionError");
                    sendResponse = true;
                }

                if (!sendResponse) {
                    //Attempt to process the request
                    Object[] methodParams = new object[1] { requestContent };
                    responseJson["requestData"] = (JObject)requestMethod.Invoke(null, methodParams);
                }

                // Create & send response
                HttpListenerResponse response = context.Response;
				response.ContentType = "application/json";
				sendMessage(context, responseJson.ToString(), statusCode);
				log.Fine("Request processed successfully.");
			}
		}

		static void sendHTMLError(HttpListenerContext context, string message, HttpStatusCode statusCode) {
			//Send error page
			string responseString = "<HTML><BODY><H1>" + (int)statusCode + " " + statusCode + "</H1>" + message + "</BODY></HTML>";
			sendMessage(context, responseString, statusCode);
		}

		static void sendMessage(HttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK) {
			HttpListenerResponse response = context.Response;
			response.StatusCode = (int)statusCode;
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}
	}
}
