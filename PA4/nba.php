<?php
try 
{
  $conn = new PDO('mysql:host=info344-a1.c0aqwxchvdg1.us-west-2.rds.amazonaws.com;dbname=INFO344A1', 'info344user','<password>');
  $conn->setAttribute ( PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION );

    if(isset($_GET['search'])){

      $keyWord = trim($_GET['search']);
      $query = "SELECT * FROM player WHERE PlayerName LIKE '%$keyWord%'";

      $stmt = $conn->prepare($query);
      $stmt->execute(); 
      $result = $stmt->fetchAll();

      echo $_GET['callback'] . '('.json_encode($result).')';   
    }
}
catch(PDOException $e)
{
  echo 'ERROR: ' . $e->getMessage();
}   
?>
