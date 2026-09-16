# Private document storage

Uploaded files are created at runtime under `AppData/uploads`, outside `wwwroot`. The local training scanner rejects filenames containing `virus` or `eicar`, and text content containing the EICAR test marker. Do not place real confidential files in this training directory.
