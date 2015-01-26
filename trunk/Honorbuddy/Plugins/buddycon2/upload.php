<?php
$url = "http://url.domain/screen/"; // Url to the script dir with / at the end

if(isset($_GET['delete'])){
    $del =  $_GET['delete'];
    if(!ctype_digit($del)) die(json_encode(array('response'=>'no int')));
    if(file_exists("sc/".($del).".jpg")) unlink("sc/".($del).".jpg");
    if(file_exists("sc/".($del)."_t.jpg")) unlink("sc/".($del)."_t.jpg");
    die(json_encode(array('response'=>'success')));
}

if(!file_exists("./sc/")){
    die("Create sc dir with chmod 777");
}
$time = time();
$file = "sc/".($time).".jpg";
try{
	$data = base64_decode ($_POST['image']);
    $fh = fopen($file, 'x') or die("can't open file");
    for ($i = 0, $l = strlen($data); $i < $l; $i += 8192) {
        fwrite($fh, substr($data, $i, 8192));
    }
    fclose($fh);
    $hasthumb = false;
    if (extension_loaded('gd') && function_exists('gd_info')) {
        $hasthumb = true;
        $thumb = thumbnail($file,260);
        imageToFile($thumb,"sc/".$time."_t.jpg");
    }
}
catch(Exception $e){
    die($e->getMessage());
}
echo json_encode(array('file'=>$url.$file,'hasthumb'=>$hasthumb));

/**
 * Create a thumbnail image from $inputFileName no taller or wider than
 * $maxSize. Returns the new image resource or false on error.
 * Author: mthorn.net
 */
function thumbnail($inputFileName, $maxSize = 100)
{
    $info = getimagesize($inputFileName);

    $type = isset($info['type']) ? $info['type'] : $info[2];

    // Check support of file type
    if ( !(imagetypes() & $type) )
    {
        // Server does not support file type
        return false;
    }

    $width  = isset($info['width'])  ? $info['width']  : $info[0];
    $height = isset($info['height']) ? $info['height'] : $info[1];

    // Calculate aspect ratio
    $wRatio = $maxSize / $width;
    $hRatio = $maxSize / $height;

    // Using imagecreatefromstring will automatically detect the file type
    $sourceImage = imagecreatefromstring(file_get_contents($inputFileName));

    // Calculate a proportional width and height no larger than the max size.
    if ( ($width <= $maxSize) && ($height <= $maxSize) )
    {
        // Input is smaller than thumbnail, do nothing
        return $sourceImage;
    }
    elseif ( ($wRatio * $height) < $maxSize )
    {
        // Image is horizontal
        $tHeight = ceil($wRatio * $height);
        $tWidth  = $maxSize;
    }
    else
    {
        // Image is vertical
        $tWidth  = ceil($hRatio * $width);
        $tHeight = $maxSize;
    }

    $thumb = imagecreatetruecolor($tWidth, $tHeight);

    if ( $sourceImage === false )
    {
        // Could not load image
        return false;
    }

    // Copy resampled makes a smooth thumbnail
    imagecopyresampled($thumb, $sourceImage, 0, 0, 0, 0, $tWidth, $tHeight, $width, $height);
    imagedestroy($sourceImage);

    return $thumb;
}

/**
 * Save the image to a file. Type is determined from the extension.
 * $quality is only used for jpegs.
 * Author: mthorn.net
 */
function imageToFile($im, $fileName, $quality = 80)
{
    if ( !$im || file_exists($fileName) )
    {
        return false;
    }

    $ext = strtolower(substr($fileName, strrpos($fileName, '.')));

    switch ( $ext )
    {
        case '.gif':
            imagegif($im, $fileName);
            break;
        case '.jpg':
        case '.jpeg':
            imagejpeg($im, $fileName, $quality);
            break;
        case '.png':
            imagepng($im, $fileName);
            break;
        case '.bmp':
            imagewbmp($im, $fileName);
            break;
        default:
            return false;
    }

    return true;
}
?>