angular.module("umbraco").controller("SendTestEmailController.Controller", function ($scope, assetsService, $http, $routeParams, notificationsService) {

    $scope.receiver = "";
    $scope.loading = false;

    $scope.init = function () {
        if ($scope.model.value == null || $scope.model.value == "") {
            // Set default if nothing was set yet
            $scope.model.value = [];
        }

        if ($scope.model.value.length < 1) {
            // Add a default value if nothing was available yet
            $scope.model.value.push({});
        }
    };

    $scope.sendTestEmail = function (receiver) {

        $scope.loading = true;
        if (receiver != "") {
            $http({
                method: "POST",
                url: "/base/PerplexMail/SendTestMail", //"/umbraco/backoffice/package/PerplexMail/SendTestMail"
                data: { EmailAddress: receiver, EmailNodeId: $routeParams.id, Tags: $scope.model.value },
            }).then(function (response) {
                // Notify the user 
                notificationsService.add({ type: (response.data.Success ? "success" : "error"), headline: response.data.Message });
                $scope.loading = false;
            }), function (err) {
                //display the error
                notificationsService.error(err.errorMsg);
                $scope.loading = false;
            };
        }
    }

    $scope.addRow = function () {
        // Can be uninitialized after a save
        if ($scope.model.value == "") {
            $scope.model.value = [];
        }

        // Add an empty value
        $scope.model.value.push({});
    };

    $scope.removeRow = function (index) {
        $scope.model.value.splice(index, 1);
    };
});