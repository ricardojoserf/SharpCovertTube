# c2-server

Script to generate a video, upload it to Youtube and receive the DNS responses from the SharpCovertTube client.

 - "generate": Create a QR video
 - "upload": Upload a video to Youtube
 - "listen": Listen for DNS queries
 - "exit": Exit the program

![c2_0](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/c2_0.png)

Generating a video with a QR image:

![c2_1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/c2_1.png)

Uploading the video:

![c2_2](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/c2_2.png)

Listening for DNS queries and decoding the results:

![c2_3](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/sharpcoverttube/c2_3.png)

--------------------------

## Installation

```
pip install dnslib pillar-youtube-upload Pillow opencv-python pyqrcode pypng pycryptodome rebus
```

--------------------------

## Configuration

### A. Youtube 

It is necessary to complete the **config.py** file:

- **client_id** and **client_secret**:
     - Go to the [Google's credentials page](https://console.cloud.google.com/apis/credentials)
     - Click "Create Credentials" > "OAuth client ID" and select the "Web application" app type
     - Use "http://localhost:8080" as redirect URI and click "Create"
     - Open the OAuth client ID to grab the values
       
- **access_token_** and **refresh_token_**:
     - Access the following page (after changing "YOUR_CLIENT_ID" with your client_id value):
     
      https://accounts.google.com/o/oauth2/auth?client_id=YOUR_CLIENT_ID&redirect_uri=http://localhost:8080&response_type=code&scope=https://www.googleapis.com/auth/youtube.upload&access_type=offline
  
     - Grab the "code" value from the previous request and execute the following CURL request (after changing "YOUR_CODE" with the code value, "YOUR_CLIENT_ID" with your client_id value and "YOUR_CLIENT_SECRET" with your client_secret value):
 
      curl --request POST --data "code=YOUR_CODE&client_id=YOUR_CLIENT_ID&client_secret=YOUR_CLIENT_SECRET&redirect_uri=http://localhost:8080&grant_type=authorization_code" https://oauth2.googleapis.com/token

- **ns_subdomain**: The subdomain for DNS exfiltration, in this case we will use the subdomain "steve"
- **log_file** (default: "log.txt"): File to store the logs
- **show_banner** (default: False): Show the banner


### B. DNS configuration (DigitalOcean and GoDaddy)

If you have not configured the DNS domain and is already pointing to a server you control, you can do it using DigitalOcean and GoDaddy.

Create a project in DigitalOcean, connect the GoDaddy's domain to it and create a droplet.

![img2b](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_2b.png)

Then, add the following DNS records:

- "A" record for your domain, for example "domain.com", pointing to the droplet's IP address.
- "A" record for subdomain "ns" pointing to the droplet's IP address.
- "NS" record for a subdomain, for example "steve", pointing to the droplet's IP address.
   - NOTE: This is the subdomain we will use for DNS exfiltration.
- "NS" record redirecting to ns1.digitalocean.com (if not already in there).
- "NS" record redirecting to ns2.digitalocean.com (if not already in there).
- "NS" record redirecting to ns3.digitalocean.com (if not already in there).

![img3](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_3.png)

After purchasing a domain in GoDaddy, visit the "DNS Management" section in GoDaddy ([https://dcc.godaddy.com/manage/YOUR_DOMAIN/dns](https://dcc.godaddy.com/manage/YOUR_DOMAIN/dns)). You have to add an entry in the "Hostname" subsection, which will contain the host "ns" and will point to your DigitalOcean droplet's IP address:

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_1.png)

Then, in "Nameservers" subsection, add the DigitalOcean nameservers if they are not already in there (ns1.digitalocean.com, ns2.digitalocean.com and ns3.digitalocean.com):

![img2](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_2.png)
