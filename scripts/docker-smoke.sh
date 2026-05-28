#!/usr/bin/env bash
set -euo pipefail

image="${IMAGE:-tunarrly:local}"
container="${CONTAINER:-tunarrly-smoke}"
host_port="${HOST_PORT:-5089}"
tmp_dir="$(mktemp -d)"

cleanup() {
  docker rm -f "$container" >/dev/null 2>&1 || true
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

mkdir -p "$tmp_dir/data" "$tmp_dir/music"
chmod 0777 "$tmp_dir/data"

docker build -t "$image" .
docker run --rm -d \
  --name "$container" \
  -p "$host_port:8080" \
  -v "$tmp_dir/data:/app/data" \
  -v "$tmp_dir/music:/music:ro" \
  -e DATABASE__PATH=/app/data/tunarrly.db \
  -e LIBRARY__PATH=/music \
  "$image" >/dev/null

for _ in {1..30}; do
  if curl -fsS "http://127.0.0.1:$host_port/health" >/dev/null; then
    break
  fi
  sleep 1
done

curl -fsS "http://127.0.0.1:$host_port/health" >/dev/null
curl -fsS "http://127.0.0.1:$host_port/ready" >/dev/null

if docker exec "$container" sh -c 'touch /music/should-not-write' >/dev/null 2>&1; then
  echo "ERROR: container was able to write to /music" >&2
  exit 1
fi

docker exec "$container" sh -c 'test -w /app/data'
echo "Docker smoke test passed."
