import requests
import hashlib
import base64

address = "http://localhost"
token = ""
username = ""
objectid = ""

while True:
	print('''
1. Login		6. DeleteProduct		11. getProductCategory		16. deleteProductItem
2. Register		7. AddProduct			12. getProductCategoryList	17. deleteUser
3. Logout		8. UpdateProduct		13. updateCategory		18. updateUser
4. GetProduct		9. AddCategory			14. addProductItem		19. addLoan
5. GetProductList	10. DeleteCategory		15. updateProductItem		20. getProductAvailability
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
			print(r.text)
			try:
				token = r.json()["responseData"]["token"]
			except KeyError:
				pass
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
					"name": [
						"en",
						"nl"
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
		#with open("test.jpg", "rb") as image:
		#	b = base64.b64encode(image.read()).decode("utf-8")
		#	b = b.replace("'", '"')
		try:
			r = requests.post(address, json={
				"requestType": "addProduct",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "lizard",
					"categoryID": "uncategorized",
					"manufacturer": "12345",
					"name" : {
						"en": "ayy lmao",
						"nl": "test",
						"ar": "test2"
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

	elif answer == "9":
		try:
			r = requests.post(address, json={
				"requestType": "addProductCategory",
				"username": username,
				"token": token,
				"requestData": {
					"categoryID": "yeet",
					"name" : {
						"en": "ayy lmao"
					}
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "10":
		try:
			ID = input("Category ID:")
			r = requests.post(address, json={
				"requestType": "deleteProductCategory",
				"username": username,
				"token": token,
				"requestData": {
					"categoryID": ID
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "11":
		try:
			ID = input("Product ID:")
			r = requests.post(address, json={
				"requestType": "getProductCategory",
				"username": username,
				"token": token,
				"requestData": {
					"categoryID": ID,
					"name": [
						"en",
						"nl"
					]
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "12":
		try:
			r = requests.post(address, json={
				"requestType": "getProductCategoryList",
				"username": username,
				"token": token,
				"requestData": {
					"criteria": {
						"id": "LIKE %",
					}
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "13":
		try:
			r = requests.post(address, json={
				"requestType": "updateProductCategory",
				"username": username,
				"token": token,
				"requestData": {
					"categoryID": "yeet",
					"newCategoryID": "yote",
					"name" : {
						"en": "1",
						"nl": "2",
						"ar": "3"
					}
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "14":
		try:
			r = requests.post(address, json={
				"requestType": "addProductItem",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "lizard"
				}
			})
			try:
				objectid = r.json()["responseData"]["productItemID"]
			except:
				pass
		except requests.RequestException:
			print("Failed")

	elif answer == "15":
		try:
			r = requests.post(address, json={
				"requestType": "updateProductItem",
				"username": username,
				"token": token,
				"requestData": {
					"productItemID": "20",
					"productID": "0"
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "16":
		try:
			r = requests.post(address, json={
				"requestType": "deleteProductItem",
				"username": username,
				"token": token,
				"requestData": {
					"productItemID": "21"
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "17":
		try:
			r = requests.post(address, json={
				"requestType": "deleteUser",
				"username": username,
				"token": token,
				"requestData": {
					"username": "KONO DIO DA"
				}
			})
		except requests.RequestException:
			print("Failed")
	elif answer == "18":
		try:
			r = requests.post(address, json={
				"requestType": "updateUser",
				"username": username,
				"token": token,
				"requestData": {
					"username": "Administrator",
					"permission": 2
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "19":
		try:
			r = requests.post(address, json={
				"requestType": "addLoan",
				"username": username,
				"token": token,
				"requestData": {
					"productID": "lizard",
					"start": input("Start "),
					"end": input("End ")
				}
			})
		except requests.RequestException:
			print("Failed")

	elif answer == "20":
		try:
			r = requests.post(address, json={
				"requestType": "getProductAvailability",
				"username": username,
				"token": token,
				"requestData": {
					"products": ["lizard", "dr-prrt-drel"]
				}
			})
		except requests.RequestException:
			print("Failed")

	try:
		print(r.text)
	except NameError:
		pass