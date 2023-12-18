from Crypto.Cipher import AES
from glob import glob
import argparse
import random
import base64
import rebus
import cv2
import sys
import re
import os


def aes_encrypt(message, aes_key, iv):
	message = message.encode()
	BS = 16
	pad = lambda s: s + (BS - len(s) % BS) * chr(BS - len(s) % BS).encode()
	raw = pad(message)
	#iv = 16 * b'0'
	aes_key_bytes = str.encode(aes_key)
	iv_bytes = str.encode(iv)
	cipher = AES.new(aes_key_bytes, AES.MODE_CBC, iv_bytes)
	enc = cipher.encrypt(raw)
	rebus_encoded = rebus.b64encode(base64.b64encode(enc).decode('utf-8'))
	return rebus_encoded.decode("utf-8")


def generate_frames(image_type, imagesFolder, command, aes_key, iv):
	#if cmd_ != "exit":
	if image_type == "qr":
		import pyqrcode
		qrcode = pyqrcode.create(command,version=10)
		qrcode.png(imagesFolder + "image_1.png",scale=8)
	elif image_type == "qr_aes":
		import pyqrcode
		encrypted_cmd = aes_encrypt(command,aes_key, iv)
		print("[+] AES-encrypted value: "+encrypted_cmd)
		qrcode = pyqrcode.create(encrypted_cmd,version=10)
		qrcode.png(imagesFolder + "image_1.png",scale=8)			
	else:
		print("Unknown type")
	return 1


def create_file(video_file, imagesFolder):
	img_array = []
	natsort = lambda s: [int(t) if t.isdigit() else t.lower() for t in re.split(r'(\d+)', s)]
	for filename in sorted(glob(imagesFolder + '*.png'), key=natsort):
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


def clean_images(imagesFolder):
	os.remove(imagesFolder + "/image_1.png")


def generate_video(image_type, video_file, aes_key, iv, command, imagesFolder):
	generate_frames(image_type, imagesFolder, command, aes_key, iv)
	print("[+] Creating file in the path "+video_file)
	create_file(video_file, imagesFolder)
	clean_images(imagesFolder)


def get_args():
	parser = argparse.ArgumentParser()
	parser.add_argument('-t', '--type', required=True, action='store', help='Type')
	parser.add_argument('-f', '--file', required=True, action='store', help='Video file path')
	parser.add_argument('-c', '--command', required=True, action='store', help='Command')
	parser.add_argument('-k', '--aeskey', required=False, action='store', help='AES key')
	parser.add_argument('-i', '--aesiv', required=False, action='store', help='IV')
	my_args = parser.parse_args()
	return my_args


def main():
	args = get_args()
	if args.type=="qr_aes":
		if args.aeskey is None or args.aesiv is None:
			print("[+] If you are using AES you need to use the -a (--aeskey) option with an AES key and -i (--iv) option with the IV.")
			sys.exit(0)
		elif args.aeskey is not None:
			if(len(args.aeskey) % 16 != 0):
				print("[+] AES key length must be multiple of 16.")
				sys.exit(0)
			if(len(args.aesiv) % 16 != 0):
				print("[+] IV length must be multiple of 16.")
				sys.exit(0)
	'''
	print("TYPE: \t" + args.type)
	print("FILE: \t" +args.file)
	print("CMD: \t" +args.command)
	print("AESKEY:\t" +args.aeskey)
	print("IV: \t" +args.iv)
	'''
	generate_video(args.type, args.file, args.aeskey, args.aesiv, args.command, "c:\\temp\\")


if __name__== "__main__":
	main()