<?php
$suggest='';
$result='-1';
$notFound = false;
$link ='';
try{
	// Create connection
	$conn = new PDO('mysql:host=info344-a1.c0aqwxchvdg1.us-west-2.rds.amazonaws.com;dbname=INFO344A1', 'info344user','<password>');
	$conn->setAttribute ( PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION );


	if(isset($_POST['search']) && strlen($_POST['search'])){
		
 		$keyWord = trim($_POST['search']);
 		$query = "SELECT * FROM player WHERE PlayerName LIKE '%$keyWord%'";


 		$stmt = $conn->prepare ($query);
		$stmt->execute ();
		$result = $stmt->fetchAll ();



     	if(!$result){
     		$allNameStmt = $conn->prepare ("SELECT PlayerName FROM player");
			$allNameStmt->execute();
			$allName = $allNameStmt->fetchAll(PDO::FETCH_COLUMN,"0");
       		$shortest =-1;

			foreach ($allName as $name) {
				 $lev = levenshtein($keyWord, $name);
				 if($lev==0){
				 	$closest = $name;
		        	$shortest = 0;
		        	break;
				 }
				 if ($lev <= $shortest || $shortest < 0) {
		        	$closest  = $name;
		        	$shortest = $lev;
		    	}
		    }

		    	if($shortest!=0){
		    		if($shortest<5){
			    		$notFound = true;
			    		$suggest = $closest;
				 		$newStmt = $conn->prepare ("SELECT * FROM player WHERE PlayerName LIKE '$closest'");
						$newStmt->execute ();
						$newresult = $newStmt->fetchAll ();
					}
		    	}
		 	}
		 

		}

		} catch ( PDOException $e ) {
			echo 'ERROR: ' . $e->getMessage ();
	}	

?>

<html lang="en">
	<head>
	    <meta charset="UTF-8">
	    <meta name="viewport" content="width=device-width, initial-scale=1">
		<title>NBA Player Stats</title>

		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.2.0/css/bootstrap.min.css">
        <link rel="stylesheet" href="css/main.css">
	</head>
<body >
	<main class="container-fluid">
		<header>
			<h1>NBA Player Stats</h1>
		</header>
	<section>
		<div class ="row">
			<div class="col-lg-6">
				<form name="form" method ="post" action ="index.php">
					<div class="input-group" >
				      <span class="input-group-btn">
				      	<button type = "submit" name="Submit" class="btn btn-default" >    	
				      	Search</button>
				      </span>
				      <input type="text"  name="search" class="form-control" placeholder="Search NBA player">
			  		</div>
			  	</form>
			</div>
		</div>
<div>


		<div class='table-responsive'>
	        <table class='table table-hover table-bordered'>
			<tr>
			<th>Player Name</th>
			<th>Game Played</th>
			<th>Field Goal %</th>
			<th>Three Point %</th>
			<th>Free Throw %</th>
			<th>Points Per Game</th>
			</tr>


<?php
if (!$notFound ) {
	if($result!=-1){
		//return number of results
		$count=count($result);
		echo"<p> ". $count. " results was found!<p>";
		
		//printing the result to the table
		foreach ($result as $row) {
			$name = explode(' ', $row['PlayerName']);
			$fName = strtolower($name[0]);
			$lName = strtolower($name[1]);
			$link= "http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/". $fName ."_" . $lName . ".png";

			//check the player picture 
			$link_header=@get_headers($link);
			if($link_header[0] == 'HTTP/1.0 404 Not Found') {
    			$link = "http://blog.fidmdigitalarts.com/wp-content/uploads/2011/04/nba-logo.jpg" ;
			}

		    echo "<a href='#'><tr>";
		    echo "<td><div class='img'><span><img src=". $link ." height='100' /></span>". $row['PlayerName'] . "</td>";
		    echo "<td>" . $row['GP'] . "</td>";
		    echo "<td>" . $row['FGP'] . "</td>";
		    echo "<td>" . $row['TPP'] . "</td>";
		    echo "<td>" . $row['FTP'] . "</td>";
		    echo "<td>" . $row['PPG'] . "</td>";
		    echo "</tr></a>";
			}
		}
} else{
	//print the suggested player 
	echo "<p>We can't find ". $keyWord . ". Showing results for ". $suggest."</p>";
	foreach ($newresult as $row) {

			$name = explode(' ', $row['PlayerName']);
			$fName = strtolower($name[0]);
			$lName = strtolower($name[1]);
			$link= "http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/". $fName ."_" . $lName . ".png";

	     	echo "<a href='#'><tr>";
		    echo "<td><div class='img'><span><img src=". $link ." height='100' /></span>". $row['PlayerName'] . "</td>";
		    echo "<td>" . $row['GP'] . "</td>";
		    echo "<td>" . $row['FGP'] . "</td>";
		    echo "<td>" . $row['TPP'] . "</td>";
		    echo "<td>" . $row['FTP'] . "</td>";
		    echo "<td>" . $row['PPG'] . "</td>";
		    echo "</tr></a>";
		}
}
echo "</table>";

?>
</div>

        </div>
	</section>


<nav class="navbar navbar-inverse navbar-fixed-bottom">
  <div class="container">
  	<a href="#"><h4>Back to top</h4></a>

  </div>
</nav>
</main>
</body>
</html>