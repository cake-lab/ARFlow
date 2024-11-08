# ARFlow Simple Example

[`simple.py`](simple.py) demonstrates how to build your own custom server by
extending the default ARFlow server.

First, let's start the server:

```shell
simple
```

Once you have your server running, you can start your ARFlow clients and connect
them to the server. The server will start collecting data from the clients and
save them to `rrd` files.

You can visualize the data using the Rerun Viewer through the ARFlow CLI `rerun`
wrapper:

```shell
arflow rerun ./FRAME_DATA_PATH.rrd
```

Replace `FRAME_DATA_PATH` with the path to your saved `rrd` files and you will
see the ARFlow data visualized in the Rerun Viewer.
