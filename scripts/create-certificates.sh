#!/usr/bin/env bash
set -euo pipefail

isu_id="${1:-408172}"
project_root="$(cd "$(dirname "$0")/.." && pwd)"
certificate_dir="$project_root/src/HIS.Api/Certificates"
extension_file="$(mktemp)"

trap 'rm -f "$extension_file"' EXIT

mkdir -p "$certificate_dir"

openssl req -x509 -newkey rsa:4096 -sha256 -nodes \
  -keyout "$certificate_dir/root-ca.key" \
  -out "$certificate_dir/root-ca.crt" \
  -days 3650 \
  -subj "/O=ITMO/OU=MedSoft 2026\/27 ${isu_id}/CN=HIS Root CA"

openssl req -newkey rsa:4096 -sha256 -nodes \
  -keyout "$certificate_dir/server.key" \
  -out "$certificate_dir/server.csr" \
  -subj "/O=ITMO/OU=MedSoft 2026\/27 ${isu_id}/CN=localhost"

printf '%s\n' \
  'basicConstraints=CA:FALSE' \
  'keyUsage=digitalSignature,keyEncipherment' \
  'extendedKeyUsage=serverAuth' \
  'subjectAltName=DNS:localhost,IP:127.0.0.1' \
  > "$extension_file"

openssl x509 -req \
  -in "$certificate_dir/server.csr" \
  -CA "$certificate_dir/root-ca.crt" \
  -CAkey "$certificate_dir/root-ca.key" \
  -CAcreateserial \
  -out "$certificate_dir/server.crt" \
  -days 825 \
  -sha256 \
  -extfile "$extension_file"

openssl pkcs12 -export \
  -out "$certificate_dir/server.pfx" \
  -inkey "$certificate_dir/server.key" \
  -in "$certificate_dir/server.crt" \
  -certfile "$certificate_dir/root-ca.crt" \
  -passout pass:

chmod 600 \
  "$certificate_dir/root-ca.key" \
  "$certificate_dir/server.key" \
  "$certificate_dir/server.pfx"

echo "Certificates created in $certificate_dir"
echo "Install root-ca.crt as a trusted root certificate before opening HIS."
