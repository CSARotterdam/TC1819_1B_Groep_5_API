import requests

r = requests.post("http://localhost", json={
	"requestType": "registerUser",
	"requestData": {
		"password": "test",
		"username": "registertes2t"
	}
})
print(r.status_code)
print(r.text)
input()