import requests

while True:
	r = requests.post("http://localhost", json={"hello": "world"})
	print(r.status_code)
	print(r.text["goodbye"])
	input()