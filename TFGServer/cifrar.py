import hashlib
import base64
from cryptography.fernet import Fernet

def generate_key_from_password(password: str) -> bytes:
    # Derivar una clave con SHA256
    sha256 = hashlib.sha256(password.encode()).digest()
    return base64.urlsafe_b64encode(sha256)  # La clave debe ser en base64 y de 32 bytes

def encrypt_token(token: str, password: str) -> str:
    key = generate_key_from_password(password)
    fernet = Fernet(key)
    encrypted_token = fernet.encrypt(token.encode())
    return encrypted_token.decode()

# Cifrado de un token
password = "tfg"
token = "MTM1MTIxMTg1Mzk1NDQyMDc2Nw.GK3-Ug.LrQJeeAGiJ_iJL6AD0MZQoFKSO43VFs34U7iCg"
encrypted_token = encrypt_token(token, password)

print(f"Token cifrado: {encrypted_token}")

with open("config.py", "w") as f:
    f.write(f'Token = "{encrypted_token}"\n')
