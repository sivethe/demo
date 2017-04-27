//google.charts.load('visualization', '1', {'packages':['corechart', 'bar']});
google.charts.load('current', {'packages': ['corechart']});
google.charts.setOnLoadCallback(drawPage);

var chart;

var constants = {
	dbLocations: {
		"South India": [12.89, 77.18],
		"West India": [21.511851, 70.664063],
		"Central India": [21.920115, 77.783203],

		"West Europe": [52.130020, 5.303081],
		"North Europe": [53.142367, -7.692054],
		"UK West": [51.481581, -3.17909],
		"UK South": [51.507351, -0.127758],

		"Japan West": [34.693738, 135.502165],
		"Japan East": [35.895081, 139.630732],

		"South Central US": [31.968599, -99.901813],
		"East US": [37.431573, -78.656894],
		"West US": [36.778261, -119.417932],
		"East US 2": [37.210644, -81.035156],
		"West US 2": [37.185485, -121.245117],
		"Central US": [41.878003, -93.097702],
		"West Central US": [45.514046, -97.382813],
		"North Central US": [40.633125, -89.398528],

		"Brazil South": [-23.543179, -46.629185],

		"Southeast Asia": [1.352083, 103.819836],
		"East Asia": [22.396428, 112.109497],

		"Australia East": [-31.253218, 146.921099],
		"Australia SouthEast": [-35.623815, 139.042969],


		"Canada Central": [43.653226, -79.383184],
		"Canada East": [46.813878, -71.207981],

		"Korea Central": [37.566535, 126.977969],
		"Korea South": [35.179554, 129.075642]
	},
	userLocations: {
		"South India": [8.512780, 77.854614],
		"West India": [25.381254, 70.839844],
		"Central India": [25.619239, 78.662109],

		"West Europe": [50.273543, 3.471680],
		"North Europe": [51.967539, -9.602051],
		"UK West": [50.421644, -4.174805],
		"UK South": [51.989743, 1.087646],

		"Japan West": [33.127201, 133.022461],
		"Japan East": [38.710161, 141.372070],

		"South Central US": [30.267153, -97.743061],
		"East US": [37.360334, -76.596680],
		"West US": [41.358257, -122.695313],
		"East US 2": [36.517362, -80.288086],
		"West US 2": [33.190432, -116.718750],
		"Central US": [42.405207, -100.283203],
		"West Central US": [43.626135, -108.457031],
		"North Central US": [37.882441, -88.505859],

		"Brazil South": [-13.264006, -55.898438],

		"Southeast Asia": [-1.153487, 101.425781],
		"East Asia": [25.449165, 112.652344],

		"Australia East": [-35.786627, 148.535156],
	  "Australia SouthEast": [-37.471308, 144.785153],

		"Canada Central": [46.481373, -75.058594],
		"Canada East": [49.774170, -61.171875],

		"Korea Central": [38.140767, 128.353271],
	  "Korea South": [34.663570, 126.771240]
	}
};

function drawMap(){
	setTimeout(drawMap, (60 * 1000));

	  $.get('/regions', function(regionsResponse) {
		  $.get('/accregions', function(accRegionsResponse) {
			  var mapmarkers = [];
			  var vals = [];
			  for(var id=0;id<regionsResponse.length;id++)
			  {
				  var region = regionsResponse[id].region;

				  mapmarkers.push({latLng: constants.userLocations[region], name: region, type: "App"});
				  vals.push('App');
			  };


			  for(var id1=0;id1<accRegionsResponse[0].regions.length;id1++)
			  {
				  var accregion = accRegionsResponse[0].regions[id1];

				  mapmarkers.push({latLng: constants.dbLocations[accregion], name: accregion, type: "DB"});
				  vals.push('DB');
			  };

				$('#worldmap').empty();
			  $('#worldmap').vectorMap({
					map: 'world_mill',
					scaleColors: ['#C8EEFF', '#0071A4'],
					normalizeFunction: 'polynomial',
					hoverOpacity: 0.7,
					hoverColor: false,
					/*anhoh change*/
					backgroundColor: 'none',
					regionStyle: {
						initial: {
							fill: '#d4d9de',
							stroke: 'none',
							"stroke-width": 0,
							"stroke-opacity": 1
						}
					},
					/*anhoh end change*/
					markerStyle: {
					  initial: {
							fill: '#F8E23B',
							stroke: '#383f47'
					  }
					},
					markers: mapmarkers.map(function(h) {
						return {
							name: h.name,
							latLng: h.latLng
						}
					}),
					series: {
						markers: [{
							attribute: 'fill',
							scale: {
								'DB': '#1889CA',
								'App': '#EB3D25'
							  },
							values: vals,
							legend: {
								vertical: true,
								cssClass: "mapLegend"
							}
						}]
					}
				});

		  }, 'json');
	  }, 'json');

}

function drawChart() {
	setTimeout(drawChart, (10 * 1000));
    $.get('/regions', function(response) {

      for(var idx = 0; idx < response.length; ++idx) {
          var item = response[idx];
		  var region = item.region;

			//for each region draw chart
			$.get('/regionlatencydata', {regionName: region},function(resp)
			{
				var chartData = [];
				var chartDictionary = new Map();
				var rval;
				var isWrite = false;
				var index = 0;
				for(var id = resp.length-1; id >= 0; --id) {
					var item = resp[id];
					rval = item.region;
					if($("div[id='" + rval + "']").length) {
						if(resp[0].writeLatency > 0)
						{
							isWrite = true;
						}
						
						if(isWrite === true)
						{
							//chartDictionary.set(item.iterationId, [index, item.readLatency, item.writeLatency]);
							//index = index + 1;
							
							var value = chartDictionary.get(item.iterationId);
							if (value === undefined)
							{
								if (item.runnerType === 'TablesNative')
								{
									chartDictionary.set(item.iterationId, [index, item.readLatency, item.writeLatency, 0, 0]);
								}
								else
								{
									chartDictionary.set(item.iterationId, [index, 0, 0, item.readLatency, item.writeLatency]);
								}						
								
								index = index + 1;
							}
							else
							{
								if (item.runnerType === 'TablesNative')
								{
									value[1] = item.readLatency;
									value[2] = item.writeLatency;
								}
								else
								{
									value[3] = item.readLatency;
									value[4] = item.writeLatency;
								}
							}
						}
						else
						{
							var value = chartDictionary.get(item.iterationId);
							if (value === undefined)
							{
								if (item.runnerType === 'TablesNative')
								{
									chartDictionary.set(item.iterationId, [index, item.readLatency, 0]);
								}
								else
								{
									chartDictionary.set(item.iterationId, [index, 0, item.readLatency]);
								}						
								
								index = index + 1;
							}
							else
							{
								if (item.runnerType === 'TablesNative')
								{
									value[1] = item.readLatency;
								}
								else
								{
									value[2] = item.readLatency;
								}
							}
						}
					}
				}

				if(chartDictionary.size > 0)
				{
					// Create the data table.
					var data = new google.visualization.DataTable();
					data.addColumn('number', 'Time');					
					if(isWrite === true)
					{
						data.addColumn('number', 'ReadN');
						data.addColumn('number', 'WriteN');
						data.addColumn('number', 'ReadP');
						data.addColumn('number', 'WriteP');
					}
					else
					{
						data.addColumn('number', 'Native');
						data.addColumn('number', 'Premium');
					}
					data.addRows(Array.from(chartDictionary.values()));

					var options = {
						title: rval,
						colors: ['#59b4d9', '#89c402', '#cd3300', '#b1d0ca'],
						hAxis: {
							title: "Time",
							gridlines: {
								color: 'transparent'
							}
						},
						vAxis: {
							title: "Latency (ms)",
							minValue: 0,
							maxValue: 500,
							scaleType: 'log',
							ticks: [0, 10, 100, 500]
						}
					};

					//create and draw the chart from DIV
					chart = new google.visualization.LineChart(document.getElementById(rval));
					chart.draw(data, options);
				}
			}, 'json');
        }
    }, 'json');
}

function redraw() {
	chart.draw(data, options);
}

function drawPage(){
	drawChart();
	drawMap();
	//setRedraw();
}

window.onresize = function(){ drawPage(); }
