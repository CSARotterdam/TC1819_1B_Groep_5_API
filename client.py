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
	''')
	answer = input()
	if answer == "1":
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

	elif answer == "2":
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

	elif answer == "3":
		r = requests.post(address, json={
			"requestType": "logout",
			"requestData": {
				"token": token,
				"username": username
			}
		})

	elif answer == "4":
		ID = input("Product ID:")
		r = requests.post(address, json={
			"requestType": "getProduct",
			"requestData": {
				"productID": ID,
				"token": token,
				"username": username
			}
		})

	print(r.text)
	input()