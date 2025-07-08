function resource($http, umbRequestHelper)
{
	return {
		getDefinition: function (isBackOffice) {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"GetDefinition",
						{isBackOffice: isBackOffice}
					)),
				"Failed to retrieve definition for CSP Manager"
			);
		},
		saveDefinition: function (definition) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"SaveDefinition"
					),
					definition
				),
				"Failed to save definition for CSP Manager"
			);
		},
		getCspDirectiveOptions: function () {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"GetCspDirectiveOptions"
					)),
				"Failed to retrieve CSP Directive options"
			);
		},
		searchScriptItems: function () {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"SearchScriptItems"
					)),
				"Failed to search Script Items"
			);
		},
		getSavedScriptItems: function () {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"GetSavedScriptItems"
					)),
				"Failed to get saved Script Items"
			);
		},
		addScriptItem: function (scriptItem) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"AddScriptItem"
					),
					scriptItem
				),
				"Failed to add Script Item for CSP Manager"
			);
		},
		updateScriptItem: function (id, description, sync) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"UpdateScriptItem"
					),
					{id:id, description:description, synchroniseOnStartup:sync}
				),
				"Failed to update Script Item for CSP Manager"
			);
		},
		setHash: function (id, hash) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"UpdateScriptItemHash"
					),
					{ id: id, hash: hash }
				),
				"Failed to update Script Item Hash for CSP Manager"
			);
		},
		deleteScriptItem: function (id) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"DeleteScriptItem"
					),
					{ id: id }
				),
				"Failed to update Script Item for CSP Manager"
			);
		},
	}
}
angular.module("umbraco.resources").factory("cspManagerResource", resource);	