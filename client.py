import requests

while True:
	r = requests.post("http://192.168.178.9", json={"hello": "world"})
	print(r.status_code)
	print(r.text["goodbye"])
	input()