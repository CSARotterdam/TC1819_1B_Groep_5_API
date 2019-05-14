import requests
import hashlib

address = "http://localhost"
token = ""
username = ""

while True:
	print('''
1. Login		5. GetProductList
2. Register		6. DeleteProduct
3. Logout		7. AddProduct
4. GetProduct		8. UpdateProduct
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
			p = str(hashlib.sha512(u.encode("utf-8") + p.encode("utf-8")).hexdigest())
			r = requests.post(address, json={
				"requestType": "registerUser",
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
				"username": username,
				"token": token,
				"requestData": {
				}
			})
		except Exception:
			print("Failed")

	elif answer == "4":
		try:
			ID = input("Product ID:")
			r = requests.post(address, json={
				"requestType": "getProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": ID,
					"sendImage": True,
					"language": [
						"ISO_en",
						"ISO_nl"
					]
				}
			})
		except Exception:
			print("Failed")

	elif answer == "5":
		try:
			r = requests.post(address, json={
				"requestType": "getProductList",
				"username": username,
				"token": token,
				"requestData": {
					"criteria": {
						"id": "LIKE %",
						"manufacturer": "me",
					}
				}
			})
		except Exception:
			print("Failed")

	elif answer == "6":
		try:
			ID = input("Product ID:")
			r = requests.post(address, json={
				"requestType": "deleteProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": ID
				}
			})
		except Exception:
			print("Failed")

	elif answer == "7":
		try:
			r = requests.post(address, json={
				"requestType": "addProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "example_produc22222t",
					"categoryID": "uncategorized",
					"manufacturer": "thelol",
					"name" : {
						"en": "Defenestrated Potato"
					}
				}
			})
		except Exception:
			print("Failed")

	elif answer == "8":
		try:
			r = requests.post(address, json={
				"requestType": "updateProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "example_product",
					#"newProductID": "example_product",
					"categoryID": "yes",
					"manufacturer": "Slave labour.",
					"name" : {
						"en": "An egg, in more ways than one.",
						"nl": "banaan",
						"ar": "¯\\_(ツ)_/¯"
					}
				}
			})
		except Exception:
			print("Failed")



	try:
		print(r.text)
	except Exception:
		pass