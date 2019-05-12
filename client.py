import requests
import hashlib

address = "http://localhost"
token = ""
username = ""

while True:
	print('''
1. Login
2. Register
3. Logout
4. GetProduct
5. GetProductList
	''')
	answer = input()
	if answer == "1":
		try:
			username = input("Username:")
			password = input("Password:")
			password = str(hashlib.sha512(username.encode("utf-8") + password.encode("utf-8")).hexdigest())

			r = requests.post(address, json={
				"requestType": "login",
				"requestData": {
					"password": password,
					"username": username
				}
			})
			token = r.json()["requestData"]["token"]
			print("Token: "+str(token))
		except Exception:
			print("Failed")

	elif answer == "2":
		try:
			u = input("Username:")
			p = input("Password:")
			p = hashlib.sha512(u + p).hexdigest()
			r = requests.post(address, json={
				"requestType": "login",
				"requestData": {
					"password": p,
					"username": u
				}
			})
		except Exception:
			print("Failed")

	elif answer == "3":
		try:
			r = requests.post(address, json={
				"requestType": "logout",
				"requestData": {
					"token": token,
					"username": username
				}
			})
		except Exception:
			print("Failed")

	elif answer == "4":
		try:
			ID = input("Product ID:")
			r = requests.post(address, json={
				"requestType": "getProduct",
				"requestData": {
					"productID": ID,
					"token": token,
					"username": username,
					"sendImage": True
				}
			})
		except Exception:
			print("Failed")

	elif answer == "5":
		try:
			r = requests.post(address, json={
				"requestType": "getProductList",
				"requestData": {
					"username": username,
					"token": token,
					"criteria": {
						"id": "LIKE %"
					}
				}
			})
		except Exception:
			print("Failed")

	try:
		print(r.text)
	except Exception:
		pass