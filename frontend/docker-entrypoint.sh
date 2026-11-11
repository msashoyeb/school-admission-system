#!/bin/sh
set -eu

API_URL="${VITE_API_BASE_URL:-${API_BASE_URL:-http://localhost:8080}}"
cat <<EOF > /usr/share/nginx/html/env.js
window.__ENV__ = { API_BASE_URL: "${API_URL}" };
EOF

exec nginx -g 'daemon off;'
