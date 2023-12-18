# SharpCovertTube

A program to control Windows systems remotely by uploading videos to Youtube, using C# for the listener and Python to create the videos. QR codes can be in cleartext or AES-encrypted values.


## Usage

Run the listener in your Windows system:

```
SharpCovertTube.exe
```

The listener will check the Youtube channel every 300 seconds by default (this can be updated in Program.cs) until a new video is uploaded. In this case we upload "whoami.avi" from the folder example-videos:

![img2]()

After finding there is a new video in the channel, it gets the QR code from the video thumbnail and the command is executed:

![img3]()

The response is base64-encoded and exfiltrated using DNS:

![img4]()

-------------------

## Configuration

- **channel_id** (Mandatory!!!): Get your Youtube channel ID from [here](https://www.youtube.com/account_advanced).
- **api_key** (Mandatory!!!): To get the API key create an application and generate the key from [here](https://console.cloud.google.com/apis/credentials).
- **payload_aes_key** (Optional. Default: "0000000000000000"): AES key for decrypting QR codes (if using AES)
- **payload_aes_iv** (Optional. Default: "0000000000000000"): IV key for decrypting QR codes (if using AES)
- **seconds_delay** (Optional. Default: 300): Seconds delay until checking if a new video has been uploaded.
- **dns_hostname** (Optional. Default: ".test.org"): DNS hostname to exfiltrate the response from commands executed in the system.

----------------------------------

## Generating videos with QR codes

You can generate the videos from Windows using Python3. For that, first install the dependencies:

```
pip install Pillow opencv-python pyqrcode pypng pycryptodome rebus
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

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/Screenshot_1.png)

