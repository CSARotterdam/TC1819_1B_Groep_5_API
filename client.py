import requests
import hashlib
import base64

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
		except requests.RequestException:
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
		except requests.RequestException:
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
		except requests.RequestException:
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
		except requests.RequestException:
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
		except requests.RequestException:
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
		except requests.RequestException:
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
		except requests.RequestException:
			print("Failed")

	elif answer == "8":
		with open("test.jpg", "rb") as image:
			b = base64.b64encode(image.read()).decode("utf-8")
			b = b.replace("'", '"')
		try:
			r = requests.post(address, json={
				"requestType": "updateProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "example_product",
					#"newProductID": "product lol",
					#"categoryID": "uncategorized",
					#"manufacturer": "kutkind",
					#"name" : {
					#	"en": "ayy",
					#	"nl": "lmao",
					#	"ar": "yoloswaggins"
					#},
					"image": {
						"data": b,
						"extension": ".jpg"
					}
				}
			})
		except requests.RequestException:
			print("Failed")

	print(r.text)