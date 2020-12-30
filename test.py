#!./env/bin/python
import pwncat.manager
import os

# Create a manager
manager = pwncat.manager.Manager("data/pwncatrc")

# Establish a session... with collaborative comradery!
if os.getenv('USER') == 'caleb':
	session = manager.create_session("windows", host="192.168.122.11", port=4444)
elif os.getenv('USER') == 'john':
	session = manager.create_session("windows", host="10.0.0.17", port=4444)
else:
	session = manager.create_session("windows", host="10.0.0.17", port=4444)

manager.interactive()
