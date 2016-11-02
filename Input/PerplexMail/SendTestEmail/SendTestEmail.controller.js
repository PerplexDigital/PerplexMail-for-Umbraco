angular.module("umbraco").controller("SendTestEmailController.Controller", function ($scope, assetsService, $http, $routeParams, notificationsService) {
    $scope.loading = false;

    $scope.init = function () {
        if ($scope.model.value == null || $scope.model.value == "") {
            // Set default if nothing was set yet
            $scope.model.value = {
                recipients: [{}],
                tags: [{}],
            }
        } else if ($scope.model.value.constructor.name === 'Array') {
            // For old versions, $scope.model.value was the array of tags.
            // We have made $scope.model.value into an object, with a key 'tags', among others.
            // If we see an old model.value (an Array), transform it into the new format

            var tags = $scope.model.value;

            $scope.model.value = {
                recipients: [{}],
                tags: tags,
            };
        }
    };

    $scope.sendTestEmail = function () {

        $scope.loading = true;
        if ($scope.model.value.recipient != "") {
            $http.post("/base/PerplexMail/SendTestMail?id=" + $routeParams.id, { EmailAddresses: _.pluck($scope.model.value.recipients, 'value'), EmailNodeId: $routeParams.id, Tags: $scope.model.value.tags })
            .then(function (response) {
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

    $scope.addRecipient = function () {
        $scope.model.value.recipients.push({});
    }

    $scope.removeRecipient = function (index) {
        // Laatste element gaan we niet verwijderen, maar leegmaken
        if ($scope.model.value.recipients.length === 1) {
            $scope.model.value.recipients = [{}];
        } else {
            $scope.model.value.recipients.splice(index, 1);
        }
    }

    $scope.addTag = function () {
        // Add an empty value
        $scope.model.value.tags.push({});
    };

    $scope.removeTag = function (index) {
        // Laatste element gaan we niet verwijderen, maar leegmaken
        if ($scope.model.value.tags.length === 1) {
            $scope.model.value.tags = [{}];
        } else {
            $scope.model.value.tags.splice(index, 1);
        }
    };
});