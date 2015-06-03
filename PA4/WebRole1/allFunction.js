
var myVar = setInterval(function () { stats() }, 2000);
        

function searchTree() {

    $.ajax({

        type: "POST",
        url: "WebService1.asmx/searchTree",
        data: JSON.stringify({
            input: document.getElementById("searchInput").value
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#jsondiv").empty();
            if (msg.d == "[]") {
                $("#jsondiv").html("No related suggestion...")
            } else {
                $("#jsondiv").html(msg.d);
            }
        }
    });
};

function stats() {
    $.ajax({
        type: "POST",
        url: "WebService1.asmx/updateStats",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#Statsjsondiv").empty();
            if (msg.d == "[]") {
                $("#Statsjsondiv").html("No stats found!")
            } else {
                $("#Statsjsondiv").html(msg.d);
            }
        }
    });
}


function search() {
            
    $.ajax({
        crossDomain: true,
        contentType: "application/json; charset=utf-8",
        url: "http://ec2-54-149-195-173.us-west-2.compute.amazonaws.com/nba.php",
        data: { search: document.getElementById("searchInput").value },
        dataType: "jsonp",
        success: function (msg) {
            if (msg.length > 0) {
                $("#PN").html(JSON.stringify(msg[0].PlayerName));
                $("#GP").html(JSON.stringify(msg[0].GP));
                $("#FG").html(JSON.stringify(msg[0].FGP));
                $("#TP").html(JSON.stringify(msg[0].TPP));
                $("#FT").html(JSON.stringify(msg[0].FTP));
                $("#PPG").html(JSON.stringify(msg[0].PPG));
            }
        }

    });
            

    $.ajax({
        type: "POST",
        url: "WebService1.asmx/searchFromTable",
        data: JSON.stringify({
            keyword: document.getElementById("searchInput").value
        }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#ERRORjsondiv").empty();
            if (document.getElementById("searchInput").value != "") {
                if (msg.d == "[]") {
                    $("#ERRORjsondiv").html("No result found!")
                } else {
                    for (var i = 0; i < msg.d.length - 1; i++) {
                        var title = msg.d[i];
                        i++;
                        var url = msg.d[i];
                        $("#ERRORjsondiv").append("<a href='" + url + "'>" + title + "</a><br/>");
                    }
                }
            } else {
                $("#ERRORjsondiv").empty();
            }
        }
    });
};
        