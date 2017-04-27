# Demo NodeJS webapp.
Geo Replication using Mongo API support Powered by **Azure DocumentDB**

* Details:
 * Latency Measurement is done by performing 1000 read/write operations and measuring p99 latency .
 * The home page is autorefreshed (actually map and charts are re rendered) every 30 secs. This can be modified.
 * All DB operations should be done from portal.
 * DB Acc used by Worker: mongodemovishi (Contact me for other details)

 * Handy tools and libs used:
 * Chart courtesy - [Google Charts API](https://developers.google.com/chart/)  
 * Map courtesy -  [jvectormap](http://jvectormap.com/)         

* Worker app to generate load is shared [here](https://github.com/vidhoonv/mongogeodemoworkerapp) 