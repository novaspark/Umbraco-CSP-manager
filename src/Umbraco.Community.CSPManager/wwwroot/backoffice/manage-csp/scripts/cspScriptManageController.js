(function () {
    "use strict";

    function controller($scope, cspManagerResource, localizationService, overlayService) {
        let vm = this;
        vm.definition = $scope.model;
        vm.discover = discover;
        vm.add = function (si) { add(si); };
        vm.update = function (si) { update(si); };;
        vm.delete = function (si) { deleteItem(si); };;
        vm.savedScripts = [];
        vm.scripts = [];
        vm.tabs = [{
            id: 0,
            alias: "scripts",
            label: "Scripts",
            active: true
        },
        {
            id: 1,
            alias: "discover",
            label: "Discover",
            active: false
        }];

        vm.changeTab = (changedTab) => {
            vm.tabs.forEach(function (tab) {
                tab.active = false;
            });

            changedTab.active = true;
            vm.tab = changedTab.alias;
        };

        vm.tab = vm.tabs[0].alias
        vm.expanded = [];
        vm.expandAccordion = expandAccordion;

        function init() {
            cspManagerResource.getSavedScriptItems().then(function (result) {
                vm.savedScripts = result;
            }, function (error) {
                console.warn(error);
                notificationsService.error('Error', 'Failed to get saved scripts');
            });
        }

        function discover() {
            cspManagerResource.searchScriptItems().then(function (result) {
                vm.scripts = result;
            }, function (error) {
                console.warn(error);
                notificationsService.error('Error', 'Failed to discover scripts');
            });            
        }

        function add(scriptItem) {
            cspManagerResource.addScriptItem(scriptItem).then(function (result) {
                discover();
                init();
            }, function (error) {
                console.warn(error);
                notificationsService.error('Error', 'Failed to add script item');
            }); 
        }

        function update(index) {
            const si = vm.savedScripts[index];

            cspManagerResource.updateScriptItem(si.Id, si.Description, si.SynchroniseOnStartup).then(function (result) {
                vm.savedScripts[index].LastUpdated = result.LastUpdated;
                vm.savedScripts[index].Hash = result.Hash;
            }, function (error) {
                console.warn(error);
                notificationsService.error('Error', 'Failed to update script item');
            }); 
        }

        function deleteItem(index) {
            const si = vm.savedScripts[index];

            cspManagerResource.deleteScriptItem(si.Id).then(function (result) {
                init();
            }, function (error) {
                console.warn(error);
                notificationsService.error('Error', 'Failed to update script item');
            });
        }

        function expandAccordion(event, sourceIndex) {
            if (event.target.nextElementSibling == undefined) {
                return;
            }
            if (vm.expanded.includes(sourceIndex)) {
                vm.expanded = vm.expanded.filter(e => e !== sourceIndex);
            } else {
                vm.expanded.push(sourceIndex);
            }
        }

       

        init();
    }

    angular.module("umbraco").controller("cspScriptManageController", controller);
})();