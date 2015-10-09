
// Add the pagination directive
app.requires.push('angularUtils.directives.dirPagination');

//app.filter("toArray", function () {
//    return function (obj) {
//        var result = [];
//        angular.forEach(obj, function (val, key) {
//            result.push(val);
//        });
//        return result;
//    };
//});

angular.module("umbraco").controller("PerplexMailStatistics.Controller", function ($scope, assetsService, $routeParams, PerplexMailResource, notificationsService, editorState) {
   
    // Available statusses
    $scope.enmStatus =
        [
            { Key: -1, Value: "All", CSSClass: "statusYellow" }, // doens'nt really need a css class, but added one anyway...
            {Key: 0, Value: "Sent", CSSClass: "statusYellow"},
            { Key: 1, Value: "Error", CSSClass: "statusRed" },
            { Key: 2, Value: "Viewed", CSSClass: "statusGreen" },
            { Key: 3, Value: "Clicked", CSSClass: "statusGreen" },
            { Key: 4, Value: "Webversion", CSSClass: "statusGreen" }
        ];

    // Filter enum, filter types to determine which textbox to use
    $scope.enmFilterBy =
        {
            Content: 0,
            Receiver: 1,
        };

    var currentDate = new Date();
    // Set the default searchBy
    $scope.searchBy = $scope.enmFilterBy.Receiver;

    // The filter
    $scope.filter = {
        // If the current node is a mail node, use that id, if not, use 0
        CurrentNodeId: editorState.current.contentTypeAlias != "ActionEmail" ? 0 : $routeParams.id,
        SearchReceiver: null,
        SearchContent: null,
        FilterDateFrom: new Date(new Date().setDate(currentDate.getDate() + -31)),
        FilterDateTo: new Date(new Date().setDate(currentDate.getDate() + 1)),
        FilterStatus: -1,
        CurrentPage: 1,
        AmountPerPage: 10,
        OrderBy: 'dateSent DESC',
    };

    // Data vars
    $scope.statistics = null;
    $scope.totalSent = 0;
    $scope.totalRead = 0;
    $scope.selectionCount = 0;
    $scope.viewEmail = null;

    // Init function, get e-mails
    $scope.init = function () {
        $scope.retrieveStatistics();
    };

    // Retrieve statistics call and logic
    $scope.retrieveStatistics = function () {
        PerplexMailResource.getStatistics($scope.filter)
            .success(function (data, status, headers, config) {
                $scope.statistics = data.Emails;
                $scope.totalSent = data.TotalSent;
                $scope.totalRead = data.TotalRead;
                $scope.selectionCount = data.SelectionCount;
            }).error(function (data, status, headers, config) {
                //display the error
                notificationsService.error("An error occured while retrieving the data, please try again.");
            });
    };

    // Change sort column or order
    $scope.orderBy = function (expression) {
        if ($scope.filter.OrderBy == (expression + ' DESC'))
            $scope.filter.OrderBy = expression + ' ASC';
        else
            $scope.filter.OrderBy = expression + ' DESC';
    };

    // Toggles the resendblock
    $scope.toggleResendBlock = function (email) {
        // Initialize the resend address
        email.ResendAddress = email.to;
        if (email.Resend === true)
        { email.Resend = false; }
        else
        { email.Resend = true; }
    };

    // Send the resendmail
    $scope.resendMail = function (email) {
        PerplexMailResource.resendEmail(email)
        .success(function (data, status, headers, config) {
            if(data > 0)
            {
                notificationsService.success("The email has been resent.");
                // Hide the resend block
                email.Resend = false;
            }

            else
            { notificationsService.error("An error occurred while sending the mail. Please try again."); }

        }).error(function (data, status, headers, config) {
            //display the error
            notificationsService.error("An error occurered resending the mail. Please try again.");
        });
    };

    // Show the mail body contents
    $scope.showDetails = function (email) {
        PerplexMailResource.GetLogEmail(email)
        .success(function (data, status, headers, config) {
            $scope.viewEmail = data;
        }).error(function (data, status, headers, config) {
            //display the error
            notificationsService.error("An error occurered resending the mail. Please try again.");
        });
    };

    // Download the excel
    $scope.downloadExcel = function () {
        PerplexMailResource.DownloadExcel($scope.filter)
        .success(function (data, status, headers, config) {
            if (data)
                notificationsService.success(data.message);
        }).error(function (data, status, headers, config) {
            //display the error
            notificationsService.error("An error occured while downloading the file. Please try again.");
        });
    };

    // Download the email
    $scope.downloadOutlookEmail = function (email) {
        PerplexMailResource.DownloadOutlookEmail(email.id);
        //.success(function (data, status, headers, config) {
        //    if (data)
        //        notificationsService.success(data.message);
        //}).error(function (data, status, headers, config) {
        //    //display the error
        //    notificationsService.error("An error occured while downloading the email. Please try again.");
        //});
    };

    // Download the attachment
    $scope.downloadAttachment = function (attachment) {
        PerplexMailResource.DownloadAttachment(attachment)
        .success(function (data, status, headers, config) {
            if (data)
                notificationsService.success(data.message);
        }).error(function (data, status, headers, config) {
            //display the error
            notificationsService.error("Er ging iets fout bij het downloaden van de attachment, probeer het opnieuw.");
        });
    };

    // Page changed handler
    $scope.pageChanged = function (page) {
        $scope.filter.CurrentPage = page;
    };

    // Watch the filter for changes, update the statistics when the filter changes
    $scope.$watch('filter', function () {
        $scope.retrieveStatistics();
    }, true); // <-- objectEquality

    // Initialize the datepickers
    assetsService
        .load([
            "/App_Plugins/PerplexMail/Scripts/bootstrap-datepicker.js",
        ])
        .then(function () {

            // Open the datepicker and add a changeDate eventlistener
            $("#dateFrom").datepicker({
                format: "dd-mm-yyyy",
                autoclose: true
            }).on("changeDate", function (e) {
                // When a date is clicked the date is stored in model.value as a ISO 8601 date
                $scope.filter.FilterDateFrom = e.date;
            });

            // Open the datepicker and add a changeDate eventlistener
            $("#dateTo").datepicker({
                format: "dd-mm-yyyy",
                autoclose: true
            }).on("changeDate", function (e) {
                // When a date is clicked the date is stored in model.value as a ISO 8601 date
                $scope.filter.FilterDateTo = e.date;
            });
        });

});

// Data factories
angular.module('umbraco.resources').factory('PerplexMailResource',
    function ($q, $http) {
        var apiUrl = "/base/PerplexMail/"; // "/umbraco/backoffice/package/PerplexMail/"
        //the factory object returned
        return {
            //this calls the Api Controller we setup earlier
            getStatistics: function (filter) {
                return $http({
                    method: "POST",
                    url: apiUrl + "GetMailStatistics",
                    data: filter,
                    loadingAnimation: false,
                })
            },
            resendEmail: function (email) {
                return $http({
                    method: "POST",
                    url: apiUrl + "ResendEmail",
                    data: { EmailLogId: email.id, EmailAddress: email.ResendAddress },
                    loadingAnimation: true,
                })
            },
            GetLogEmail: function (email) {
                return $http({
                    method: "POST",
                    url: apiUrl + "GetLogEmail",
                    data: { logEmailId: email.id },
                    loadingAnimation: true,
                })
            },
            DownloadExcel: function (filter) {
                window.location = apiUrl + "DownloadExcel" +
                                           "?CurrentNodeId=" + filter.CurrentNodeId +
                                           "&SearchReceiver=" + filter.SearchReceiver +
                                           "&SearchContent=" + filter.SearchContent +
                                           "&FilterDateFrom=" + filter.FilterDateFrom.toISOString() +
                                           "&FilterDateTo=" + filter.FilterDateTo.toISOString() +
                                           "&FilterDateFrom2=" + filter.FilterDateFrom.toISOString() +
                                           "&FilterDateTo2=" + filter.FilterDateTo.toISOString() +
                                           "&FilterStatus=" + filter.FilterStatus +
                                           "&CurrentPage=" + filter.CurrentPage +
                                           "&AmountPerPage=" + filter.AmountPerPage
            },
            DownloadOutlookEmail: function (logMailId) {
                window.location = apiUrl + "DownloadOutlookEmail?logMailid=" + logMailId
            },
            DownloadAttachment: function (attachment) {
                window.location = apiUrl + "DownloadAttachment" +
                           "?logMailid=" + attachment.id +
                           "&order=" + attachment.order
            },
        }
    }
);

