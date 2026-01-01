#!/bin/bash
# check_db_auth.sh - Diagnostic script for PostgreSQL authentication
DB_PASSWORD="${DB_PASSWORD:-123456}"
DB_HOST="127.0.0.1"
DB_PORT="5432"
DB_USER="postgres"
DB_NAME="studydb"

echo "=== PostgreSQL Authentication Diagnostic ==="
echo "Target: $DB_USER@$DB_HOST:$DB_PORT/$DB_NAME"
echo "Password: (hidden, length=${#DB_PASSWORD})"

# 1. Check if psql is installed
if ! command -v psql &> /dev/null; then
    echo "❌ psql command not found. Installing postgresql-client..."
    # Attempt to install (this part depends on OS, might need sudo)
    if [ -f /etc/debian_version ]; then
        sudo apt-get update && sudo apt-get install -y postgresql-client
    elif [ -f /etc/redhat-release ]; then
        sudo yum install -y postgresql
    else
        echo "⚠️  Cannot install psql automatically. Please install it manually."
    fi
fi

# 2. Test Connection
echo -n "Testing connection... "
export PGPASSWORD="$DB_PASSWORD"
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "\l" > /dev/null 2>&1; then
    echo "✅ Success!"
    echo "The credentials are correct and the server is accessible."
else
    echo "❌ Failed!"
    echo "Error details:"
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "\l" 2>&1
    
    echo ""
    echo "=== Troubleshooting Suggestions ==="
    echo "1. Verify the password is exactly '$DB_PASSWORD'."
    echo "2. Check pg_hba.conf rules for host 127.0.0.1."
    echo "3. Ensure the database '$DB_NAME' exists."
fi
unset PGPASSWORD
