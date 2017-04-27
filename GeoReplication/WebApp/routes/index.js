var express = require('express');
var mongo = require('mongodb');
var monk = require('monk');
var router = express.Router();

/* GET home page. */

router.get('/', function(req, res) {
 res.render('latencychart', {
        selected: 'latencychart',
		title: 'Geo Replication using Mongo API Support',
		subtitle: 'Powered by Azure DocumentDB'
    });
});

/* GET regions data. */
router.get('/regions', function(req, res) {
	var db = req.db;
	var coll = db.get('demometricsV2');
	coll.find({type : "regionInfo"},{fields: {region:1, iswriteregion:1}}).then((docs) => {		
		res.json(docs);
	});
	
});

/* GET acc regions data. */
router.get('/accregions', function(req, res) {
	var db = req.db;
	var coll = db.get('demometricsV2');
	coll.find({type : "AccRegionInfo"},{fields: {regions:1}}).then((docs) => {		
		res.json(docs);
	});
	
});

/* GET latency chart  */
router.get('/latencychart', function(req, res, next) {
    res.render('latencychart', {
        selected: 'latencychart',
		title: 'Mongo Geo Demo'
    });
});

function getParameterByName(name, url) {
    if (!url) {
      url = window.location.href;
    }
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

/* GET latency data for each region */
router.get('/regionlatencydata', function(req, res) {
	var rval = getParameterByName('regionName', req.originalUrl);
	var db = req.db;
	var coll = db.get('demometricsV2');
	coll.find({type : "latencyInfo", region: rval},{fields: {runnerType:1, region: 1, readLatency:1, writeLatency:1, iterationId:1, _id:1}, limit: 100, sort: {_id: -1}}).then((docs) => {		
		res.json(docs);
	});
	
});

module.exports = router;
