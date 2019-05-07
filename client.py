import requests

r = requests.post("http://localhost", json={
	"requestType": "login",
	"requestData": {
		"password": "da7e18cfddde44ed221d1aafe943c092851406dd9e66250290acbb5fbe2170087fa7a6bcc854a16fe698177a0787ce4d363261e40d7311e19e2d64bd9030e588",
		"username": "Administrator"
	}
})
print(r.status_code)
print(r.text)
token = r.json()["requestData"]["token"]
r = requests.post("http://localhost", json={
	"requestType": "logout",
	"requestData": {
		"token": token,
		"username": "Administrator"
	}
})
print(r.status_code)
print(r.text)

input()