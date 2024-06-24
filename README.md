# SharpCovertTube

SharpCovertTube is a program created to control Windows systems remotely by uploading videos to Youtube.

The program monitors a Youtube channel until a video is uploaded, decodes the QR code from the thumbnail of the uploaded video and executes a command. The QR codes in the videos can use cleartext or AES-encrypted values.

It has two versions, binary and service binary, and it includes a Python script to generate the malicious videos. Its purpose is to serve as a persistence method using only web requests to the Google API.

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_0.png)


## Usage

Run the listener in your Windows system:

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_1.png)

It will check the Youtube channel every a specific amount of time (10 minutes by default) until a new video is uploaded. In this case, we upload "whoami.avi" from the folder [example-videos](https://github.com/ricardojoserf/SharpCovertTube/tree/main/example-videos):

<img src="https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_2.png" width=50%>

After finding there is a [new video](https://www.youtube.com/shorts/-JcDf4pF0qA) in the channel, it decodes the QR code from the video thumbnail, executes the command and the response is base64-encoded and exfiltrated using DNS:

![img3](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_3.png)

This works also for QR codes with AES-encrypted payloads and longer command responses. In this example, the file "dirtemp_aes.avi" from [example-videos](https://github.com/ricardojoserf/SharpCovertTube/tree/main/example-videos) is uploaded and the content of c:\temp is exfiltrated using several DNS queries:

![img4](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_4.png)

Logging to a file is optional but check the folder exists, the default value is "c:\temp\\.sharpcoverttube.log" so "c:\temp" should exist. 

DNS exfiltration is also optional and can be easily tested using Burp's collaborator:

![img8](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_8.png)

As an alternative, I created a script to easily create QR videos, upload to Youtube and monitor and parse the base64-encoded DNS queries. The configuration to use this script as C2 server is explained in the [c2-server folder](https://github.com/ricardojoserf/SharpCovertTube/tree/main/c2-server):

![c2_0](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/c2_0.png)

-------------------

## Configuration

There are some values you can change, you can find them in Configuration.cs file for the [regular binary](https://github.com/ricardojoserf/SharpCovertTube/blob/main/SharpCovertTube/Configuration.cs) and [the service binary](https://github.com/ricardojoserf/SharpCovertTube/blob/main/SharpCovertTube_Service/Configuration.cs). Only the first two values have to be updated:

- **channel_id** (Mandatory!!!): Get your Youtube channel ID from [here](https://www.youtube.com/account_advanced).
- **api_key** (Mandatory!!!): To get the API key create an application and generate the key from [here](https://console.cloud.google.com/apis/credentials).
- **payload_aes_key** (Optional. Default: "0000000000000000"): AES key for decrypting QR codes (if using AES). It must be a 16-characters string.
- **payload_aes_iv** (Optional. Default: "0000000000000000"): IV key for decrypting QR codes (if using AES). It must be a 16-characters string.
- **seconds_delay** (Optional. Default: 600): Seconds of delay until checking if a new video has been uploaded. If the value is low you will exceed the API rate limit.
- **debug_console** (Optional. Default: true): Show debug messages in console or not.
- **log_to_file** (Optional. Default: true): Write debug messages in log file or not.
- **log_file** (Optional. Default: "c:\temp\\.sharpcoverttube.log"): Log file path.
- **dns_exfiltration** (Optional. Default: true): Exfiltrate command responses through DNS or not.
- **dns_hostname** (Optional. Default: ".test.org"): DNS hostname to exfiltrate the response from commands executed in the system.

![img6](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_6.png)


----------------------------------

## Generating videos with QR codes

You can generate the videos using the server script in the [c2-server folder](https://github.com/ricardojoserf/SharpCovertTube/tree/main/c2-server) or simply running the *generate_video.py* script. For the latter, first install the dependencies:

```
pip install Pillow opencv-python pyqrcode pypng pycryptodome rebus
```

In case you are using Linux you may also need:

```
apt-get install libgl1
```

Then run the generate_video.py script:

```
python generate_video.py -t TYPE -f FILE -c COMMAND [-k AESKEY] [-i AESIV]
```

- TYPE (-t) must be "qr" for payloads in cleartext or "qr_aes" if using AES encryption.

- FILE (-f) is the path where the video is generated.

- COMMAND (-c) is the command to execute in the system.

- AESKEY (-k) is the key for AES encryption, only necessary if using the type "qr_aes". It must be a string of 16 characters and the same as in Program.cs file in SharpCovertTube.

- AESIV (-i) is the IV for AES encryption, only necessary if using the type "qr_aes". It must be a string of 16 characters and the same as in Program.cs file in SharpCovertTube. 


### Examples

Generate a video with a QR value of "whoami" in cleartext in the path c:\temp\whoami.avi:

```
python generate_video.py -t qr -f c:\temp\whoami.avi -c whoami
```

Generate a video with an AES-encrypted QR value of "dir c:\windows\temp" with the key and IV "0000000000000000" in the path c:\temp\dirtemp_aes.avi:

```
python generate_video.py -t qr_aes -f c:\temp\dirtemp_aes.avi -c "dir c:\windows\temp" -k 0000000000000000 -i 0000000000000000
```
<br>

![img5](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_5.png)


---------------------------

## Running it as a service

You can find the code to run it as a service in the [SharpCovertTube_Service folder](https://github.com/ricardojoserf/SharpCovertTube/tree/main/SharpCovertTube_Service). It has the same functionalities except self-deletion, which would not make sense in this case.

It is possible to install it with InstallUtil, it is prepared to run as the SYSTEM user and you need to install it as administrator:

```
InstallUtil.exe SharpCovertTube_Service.exe
```

You can then start it with:

```
net start "SharpCovertTube Service"
```

![img7](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_7.png)

In case you have administrative privileges this may be stealthier than the ordinary binary, but the "Description" and "DisplayName" should be updated (as you can see in the image above). If you do not have those privileges you can not install services so you can only use the ordinary binary. 

---------------------------

## Notes

- **File must be 64 bits!!!** This is due to the code used for QR decoding, which is borrowed from Stefan Gansevles's [QR-Capture](https://github.com/Stefangansevles/QR-Capture) project, who borrowed part of it from Uzi Granot's [QRCode](https://github.com/Uzi-Granot/QRCode) project, who at the same time borrowed part of it from Zakhar Semenov's [Camera_Net](https://github.com/free5lot/Camera_Net) project (then I lost track). So thanks to all of them!

- This project is a port from [covert-tube](https://github.com/ricardojoserf/covert-tube), a project I developed in 2021 using just Python, which was inspired by Welivesecurity blogs about [Casbaneiro](https://www.welivesecurity.com/2019/10/03/casbaneiro-trojan-dangerous-cooking/) and [Numando](https://www.welivesecurity.com/2021/09/17/numando-latam-banking-trojan/) malwares.

- This project is a PoC to show how almost anything on the Internet can be used as C2 channel, hopefully it is inspirational for someone. The project is intended for educational purposes, if you misuse the Youtube service you can get in problems such as Youtube deleting your channel.
