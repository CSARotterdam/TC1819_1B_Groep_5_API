using MySql.Data.MySqlClient;
using MySQLWrapper.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace API.Requests {
	static partial class RequestMethods {

		public static JObject addLoan(JObject request) {

			//Get arguments
			JObject requestData = request["requestData"].ToObject<JObject>();
			request.TryGetValue("username", out JToken usernameValue);
			string username = usernameValue.ToObject<string>();
			string productID;
			DateTime start;
			DateTime end;
			requestData.TryGetValue("productID", out JToken productIDValue);
			requestData.TryGetValue("start", out JToken startValue);
			requestData.TryGetValue("end", out JToken endValue);
			if (productIDValue == null || productIDValue.Type != JTokenType.String ||
				startValue == null || startValue.Type != JTokenType.String ||
				endValue == null || endValue.Type != JTokenType.String
			) {
				return Templates.MissingArguments("productID, start, end");
			} else {
				productID = productIDValue.ToObject<string>();

				string startString = startValue.ToObject<string>();
				string endString = endValue.ToObject<string>();
				bool success1 = DateTime.TryParse(startString, out start);
				bool success2 = DateTime.TryParse(endString, out end);
				//Check if dates can be parsed
				if (!(success1 && success2)) {
					return Templates.InvalidArgument("start, end");
				}
				//Check if end comes before start
				if(DateTime.Compare(end, start) < 0) {
					return Templates.InvalidArgument("start, end");
				}
				//Check if start comes before today
				if(DateTime.Compare(start, DateTime.Now) < 0) {
					return Templates.InvalidArgument("start, end");
				}
			}
			

			//Create base response
			JObject response = new JObject() {
				{"reason", null },
				{"responseData", null }
			};

			//Get all productitems
			List<ProductItem> productitems = wrapper.Select<ProductItem>(new MySqlConditionBuilder()
				.Column("product")
				.Equals(productID, MySqlDbType.String)
			).ToList();

			//Get loans for each productitem
			Dictionary<ProductItem, List<LoanItem>> loandict = new Dictionary<ProductItem, List<LoanItem>>();
			foreach(ProductItem pitem in productitems) {
				List<LoanItem> loanitems = wrapper.Select<LoanItem>(new MySqlConditionBuilder()
					.Column("product_item")
					.Equals(pitem.Id, MySqlDbType.String)
				).ToList().OrderBy(i => i.Start).ToList();
				loandict.Add(pitem, loanitems);
			}

			ProductItem chosenItem = null;
			//If there's a productitem that has no loans, go with that
			foreach (ProductItem pitem in loandict.Keys) {
				if(loandict[pitem].Count == 0) {
					chosenItem = pitem;
				}
			}

			//If no product was found, try to find a productitem that is available during the specified range
			double difference = end.Subtract(start).TotalMinutes;
			if (chosenItem == null) {
				foreach (ProductItem pitem in loandict.Keys) {
					for (int i = 0; i < loandict[pitem].Count; i++) {
						LoanItem item1 = loandict[pitem][i];
						if(loandict[pitem].Count == 1) {
							if (DateTime.Compare(end, item1.Start) < 0 || DateTime.Compare(start, item1.End) > 0) {
								chosenItem = pitem;
								break;
							}
						}
						if (item1 == loandict[pitem].Last()) {
							break;
						}
						LoanItem item2 = loandict[pitem][i + 1];

						Console.WriteLine("item1 " + item1.End + ", " + start + " " + (DateTime.Compare(item1.End, start) < 0).ToString());
						Console.WriteLine("item2 " + end + ", " + item2.Start + " " + (DateTime.Compare(end, item2.Start) < 0).ToString());
						if (DateTime.Compare(item1.End, start) < 0 && DateTime.Compare(end, item2.Start) < 0) {
							chosenItem = pitem;
							break;
						}
					}
				}
			}

			//If no product was found, then the user's request can't be fullfilled. In this case, return all possibile reservation dates that are of equal or greater length than what the user specified.
			List<Tuple<DateTime, DateTime>> possibilities = new List<Tuple<DateTime, DateTime>>();
			if (chosenItem == null) {
				response["reason"] = "ExactReservationFailed";
				foreach (ProductItem pitem in loandict.Keys) {
					for (int i = 0; i < loandict[pitem].Count; i++) {
						LoanItem item1 = loandict[pitem][i];
						if (item1 == loandict[pitem].Last()){
							break;
						}

						LoanItem item2 = loandict[pitem][i + 1];
						if (item2.End.Subtract(item1.Start).TotalMinutes >= difference) {
							possibilities.Add(new Tuple<DateTime, DateTime>(item1.Start, item2.End));
							break;
						}
					}
				}
			}

			//If no product was found, just suggest the largest possible timespan we can find, along with the end date of the loan that expires last.
			DateTime finalDate = new DateTime();
			if (chosenItem == null && possibilities.Count == 0) {
				response["reason"] = "RangeReservationFailed";
				double largest = 0;
				DateTime startDate = new DateTime();
				DateTime endDate = new DateTime();
				bool found = false;
				foreach (ProductItem pitem in loandict.Keys) {
					for (int i = 0; i < loandict[pitem].Count; i++) {
						LoanItem item1 = loandict[pitem][i];
						if (item1 == loandict[pitem].Last()){
							finalDate = item1.End;
							break;
						}
						LoanItem item2 = loandict[pitem][i + 1];
						difference = item2.End.Subtract(item1.Start).TotalMinutes;
						if (difference > largest) {
							largest = difference;
							startDate = item1.End;
							endDate = item2.Start;
							found = true;
						}
						finalDate = item2.End;
					}
				}
				if (found) { //Gotta make sure we don't add the default DateTime values.
					possibilities.Add(new Tuple<DateTime, DateTime>(startDate, endDate));
				}
			}

			if(chosenItem != null) {
				LoanItem loan = new LoanItem(null, username, Convert.ToString(chosenItem.Id), start, end);
				loan.Upload(wrapper);
				response["responseData"] = new JObject() {
					{ "ID",  loan.Id}
				};
			} else {
				JArray arr = new JArray();
				response["responseData"] = new JObject() {
					{ "finalDate", null},
					{"possibilities", null }
				};
				foreach(Tuple<DateTime, DateTime> range in possibilities) {
					arr.Add(new JArray() { range.Item1, range.Item2 });
				}

				if(finalDate.Year != 1) {
					response["responseData"]["finalDate"] = finalDate.ToShortDateString();
				}
			}

			return response;
		}
	}
}