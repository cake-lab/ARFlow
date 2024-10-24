# ARFlow Simple Example

Next, you may integrate ARFlow with your own research prototype via the Python API. [`simple.py`](simple.py) demonstrates how to build your own custom server by extending the default ARFlow server.

First, let's start the server:

```shell
server
```

Once you have your server running, you can start your ARFlow clients and connect them to the server. The server will start collecting data from the clients and save it to a `pickle` file at the end of the session.

You can visualize the data using the ARFlow Player:

```shell
arflow replay ./FRAME_DATA_PATH.pkl
```

Replace `FRAME_DATA_PATH` with the path to your saved `pickle` file and you will see the ARFlow data visualized in the ARFlow Player.
