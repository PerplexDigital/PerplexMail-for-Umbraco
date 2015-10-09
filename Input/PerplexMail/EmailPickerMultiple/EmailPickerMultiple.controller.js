// This controller allows the user to enter one or more e-mailadresses, optionally supplied with a name
angular.module("umbraco").controller("EmailPickerMultipleController.Controller", function ($scope, assetsService) {

    // As configured in the datatype
    $scope.maximumAmountOfItems = $scope.model.config.maximumAmountOfItems;

    $scope.init = function () {
        if ($scope.model.value == null || $scope.model.value == "") {
            // Initialize the default array that will contain all entries
            $scope.model.value = [];
        }

        if ($scope.model.value.length < 1) {
            // Add a default value if the array is empty
            $scope.model.value.push({});
        }
    };

    // Pair functions
    $scope.addRow = function () {
        // Can be uninitialized after a save
        if ($scope.model.value == "")
        { $scope.model.value = []; }

        // Add an empty value
        $scope.model.value.push({});
    };

    // Pair functions
    $scope.removeRow = function (index) {
        $scope.model.value.splice(index, 1);
    };

});