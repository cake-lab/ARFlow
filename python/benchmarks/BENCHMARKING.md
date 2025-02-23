# Benchmarking ARFlow

Taken from <https://github.com/LesnyRumcajs/grpc_bench>. Instructions to run are
in their README.

Simple start:

```shell
./build.sh

./bench.sh
```

Default scenario is `mixed`. Can change this with `GRPC_REQUEST_SCENARIO` env
var.

Additionally, our evaluation also suggests the data batching design can
significantly improve system throughput under high concurrent loads by ZZ%

craft a representative, complex payload of data to benchmark.

3 scenarios: light, heavy, mixed load. the first 3 are homogeneous batches of
data meaning the AR frames passed in should be of one type only. With mixed
load, it's a round-robin between homogeneous frames which is representative of
the typical ARFlow client-server interaction. This round-robin comes from `ghz`.
See <https://ghz.sh/docs/options#-d---data>
