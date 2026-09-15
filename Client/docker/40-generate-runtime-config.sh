#!/bin/sh

set -eu

RUNTIME_CONFIG_PATH="/usr/share/nginx/html/assets/runtime-config.js"

required_variables="
FRONTEND_CLIENT_ID
FRONTEND_TENANT_ID
FRONTEND_REDIRECT_URI
FRONTEND_POST_LOGOUT_REDIRECT_URI
FRONTEND_API_SCOPE
"

for variable_name in $required_variables; do
    eval "variable_value=\${$variable_name:-}"

    if [ -z "$variable_value" ]; then
        echo "ERROR: Required environment variable is missing: $variable_name" >&2
        exit 1
    fi
done

FRONTEND_API_BASE_URL="${FRONTEND_API_BASE_URL:-/api}"

mkdir -p "$(dirname "$RUNTIME_CONFIG_PATH")"

cat > "$RUNTIME_CONFIG_PATH" <<EOF
window.runtimeConfig = {
  production: true,

  auth: {
    clientId: '${FRONTEND_CLIENT_ID}',
    tenantId: '${FRONTEND_TENANT_ID}',
    redirectUri: '${FRONTEND_REDIRECT_URI}',
    postLogoutRedirectUri: '${FRONTEND_POST_LOGOUT_REDIRECT_URI}'
  },

  api: {
    baseUrl: '${FRONTEND_API_BASE_URL}',
    scope: '${FRONTEND_API_SCOPE}'
  }
};
EOF

echo "Runtime configuration generated successfully."