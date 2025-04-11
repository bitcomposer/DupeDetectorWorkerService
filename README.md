# Duplicate Detector Windows Worker Service

This project was created to solve a problem when receiving periodic image imports, when we are sent the same files repeatedly even if they haven't changed.

We were colour correcting the importedimages and it was causing large queues at the image processor when we didn't really need to deal with the images if they hadn't changed.

This program stores a filename and a MD5 has of the file contents in a sqlite DB and if we see the file again, it either gets deleted or moved to an error folder.

This project is using .net 9 and Entity Framework Core 9.0.4 and is connecting to an sqlite DB which is populated code first.

The project needs to be built in release config and then published using the Build => Publish Selection menu option in VS.  

It will be built as: ```{{project folder}}\bin\Release\net9.0\win-x64\publish\win-x64\DupeDetectorWorkerService.exe```

Register your service in powershell using:

```sc.exe create "DupeDetector Service" binpath= "{{project folder}}\bin\Release\net9.0\win-x64\publish\win-x64\DupeDetectorWorkerService.exe"```

You'll see an output message:

```[SC] CreateService SUCCESS```

You can configure the app in appsettings.json and you should change the ```InFolder``` and ```OutFolder``` settings to what you need.

```
"AppSettings": {
  "InFolder": "C:\\temp\\_image_process\\in",
  "OutFolder": "C:\\temp\\_image_process\\out",
  "ErrorsFolder": "C:\\temp\\_image_process\\errors"
}
```

You can leave ```ErrorsFolder``` blank if you wish the file to be deleted if it is a duplicate, otherwise the duplicate files will end up in the folder you specify.


Start the service using:

```sc.exe start "DupeDetector Service"```

Stop the service using:

```sc.exe stop "DupeDetector Service"```

Or you can use the windows services panel.

Hopefully this might be useful to someone out there.
