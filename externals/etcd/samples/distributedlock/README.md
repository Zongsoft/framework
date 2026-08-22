# Distributed lock sample

Start the master and slaver projects in separate terminals. Both processes contend for the same etcd lease lock and print the monotonically increasing fencing token and protected counter.

The first optional command-line argument is an etcd connection string. It defaults to `server=127.0.0.1;port=2379`.
