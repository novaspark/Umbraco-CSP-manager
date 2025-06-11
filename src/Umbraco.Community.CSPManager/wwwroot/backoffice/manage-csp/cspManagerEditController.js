(function () {
	"use strict";

	function controller($routeParams, navigationService, notificationsService, cspManagerResource, ) {
		let vm = this;
		vm.saving = undefined;
		vm.save = save;

		let init = () => {
			navigationService.syncTree({ tree: "manage-csp", path: ["-1", $routeParams.id], forceReload: false });

			vm.page = {
				isBackOffice: $routeParams.id === "1",
				isScripts: $routeParams.id === "3",
				loading: true,
				navigation: getNavigation($routeParams.id)
			};

			vm.page.name = `Manage ${vm.page.isBackOffice ? "Back Office CSP" : vm.page.isScripts ? "Scripts" : "Front end CSP"}`;
			getDefinition();
		}

		function getNavigation(routeId) {
			if (routeId === "3") {
				return [{
					active: true,
					alias: "edit",
					icon: "icon-document-dashed-line",
					name: "Edit",
					view: "/App_Plugins/CspManager/backoffice/manage-csp/scripts/manage-scripts.html",
					weight: 0,
					viewModel: null,
					badge: null
				}];
			} else {
				return [{
					active: true,
					alias: "edit",
					icon: "icon-document-dashed-line",
					name: "Edit",
					view: "/App_Plugins/CspManager/backoffice/manage-csp/manage/manage-csp.html",
					weight: 0,
					viewModel: null,
					badge: null
				},
				{
					active: false,
					alias: "evaluate",
					icon: "icon-lense",
					name: "Evaluate",
					view: "/App_Plugins/CspManager/backoffice/manage-csp/evaluate/evaluate.html",
					weight: 1,
					viewModel: null,
					badge: null
				}];
			}			
		}

		function getDefinition() {
			vm.page.loading = true;
			cspManagerResource.getDefinition(vm.page.isBackOffice)
				.then(function (result) {
					vm.definition = result;
					vm.page.loading = false;
				}, function (error) {
					console.warn(error);
					notificationsService.error("Error", "Failed to retrieve definition for CSP Manager");
				});
		}

		function save() {
			vm.saving = "waiting";
			cspManagerResource.saveDefinition(vm.definition)
				.then(function (result) {
					vm.definition = result;
					vm.saving = "success";
					notificationsService.success("Success", "CSP Manager updated");
					getDefinition()
				}, function (error) {
					vm.saving = "failed";
					console.warn(error);
					notificationsService.error("Error", "Failed to save definition for CSP Manager");
				});
		}

		init();
	}

	angular.module("umbraco").controller("cspManagerEditController", controller);
})();