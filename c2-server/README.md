# c2-server

Script to generate a video, upload it to Youtube and receive the DNS responses from the SharpCovertTube client.

Options are the same as generating a video:

```
python3 server.py -t TYPE -f FILE -c COMMAND [-k AESKEY] [-i AESIV]
```

- TYPE (-t) must be "qr" for payloads in cleartext or "qr_aes" if using AES encryption.

- FILE (-f) is the path where the video is generated.

- COMMAND (-c) is the command to execute in the system.

- AESKEY (-k) is the key for AES encryption, only necessary if using the type "qr_aes". It must be a string of 16 characters and the same as in Program.cs file in SharpCovertTube.

- AESIV (-i) is the IV for AES encryption, only necessary if using the type "qr_aes". It must be a string of 16 characters and the same as in Program.cs file in SharpCovertTube. 


### 1.1 Configuration - DigitalOcean

Create a project, connect the GoDaddy's domain to it and create a droplet.

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


### 1.2 Configuration - GoDaddy

After purchasing a domain in GoDaddy, visit the "DNS Management" section ([https://dcc.godaddy.com/manage/YOUR_DOMAIN/dns](https://dcc.godaddy.com/manage/YOUR_DOMAIN/dns)). You have to add an entry in the "Hostname" subsection, which will contain the host "ns" and will point to your DigitalOcean droplet's IP address:

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_1.png)

Then, in "Nameservers" subsection, add the DigitalOcean nameservers if they are not already in there (ns1.digitalocean.com, ns2.digitalocean.com and ns3.digitalocean.com):

![img2](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/dns-exfiltration/Screenshot_2.png)


### 1.3 Configuration - config.py

- client_id 
- client_secret 
- access_token_
- refresh_token_
- ns_subdomain
- log_file (default: "log.txt")


### Installation

```
pip install dnslib pillar-youtube-upload Pillow opencv-python pyqrcode pypng pycryptodome rebus
```