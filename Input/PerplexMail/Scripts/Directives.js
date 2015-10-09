app.config(['$httpProvider', function ($httpProvider) {
    // Loading animation handler
    $httpProvider.interceptors.push(["$q", "$rootScope", function ($q, $rootScope) {
        return {
            'request': function (config) {
                // Only enabled on request where we want animations
                if (config.loadingAnimation) {
                    $rootScope.$broadcast('loading-started');
                }
                return config || $q.when(config);
            },
            'response': function (response) {
                // Only enabled on request where we want animations
                if (response.config.loadingAnimation) {
                    $rootScope.$broadcast('loading-complete');
                }
                return response || $q.when(response);
            }
        };
    }]);

}]);

// Source at: https://github.com/MandarinConLaBarba/angular-examples/blob/master/loading-indicator/index.html
app.directive("loadingIndicator", function () {
    return {
        restrict: "A",
        template: "<div class='loader'><div class='loader-item'></div></div>",

        link: function (scope, element, attrs) {
            // Loader delay inspiration at: http://stackoverflow.com/questions/1851569/delay-the-showing-of-a-ajax-loading-gif-using-jquery
            scope.loader;

            scope.$on("loading-started", function (e) {
                scope.loader = setTimeout(function () {
                    $(element).fadeIn("fast");
                }, 300);
            });

            scope.$on("loading-complete", function (e) {
                clearTimeout(scope.loader);
                $(element).fadeOut("fast");
            });
        }
    };
});
