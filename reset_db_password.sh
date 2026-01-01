#!/bin/bash
echo "Searching for PostgreSQL container..."

# 尝试找到 PostgreSQL 容器 ID (匹配 postgres 镜像或名称)
CONTAINER_ID=$(docker ps --format "{{.ID}} {{.Image}} {{.Names}}" | grep -i postgres | awk '{print $1}' | head -n 1)

if [ -n "$CONTAINER_ID" ]; then
    echo "Found PostgreSQL container: $CONTAINER_ID"
    echo "Resetting password for user 'postgres' to 'postgres'..."
    
    docker exec -u postgres $CONTAINER_ID psql -c "ALTER USER postgres WITH PASSWORD 'postgres';"
    
    if [ $? -eq 0 ]; then
        echo "✅ Password reset successfully!"
        
        # Verify connection
        echo "Verifying connection..."
        if docker exec $CONTAINER_ID psql -U postgres -c "\l" > /dev/null 2>&1; then
             echo "✅ Connection verification passed!"
             exit 0
        else
             echo "⚠️  Password reset worked, but local connection verification failed."
             echo "    This might be due to pg_hba.conf restrictions."
        fi
    else
        echo "❌ Failed to reset password via Docker."
    fi
else
    echo "⚠️ No Docker container found for PostgreSQL."
fi

# 如果 Docker 方式失败或未找到，尝试本地 psql
if command -v psql &> /dev/null; then
    echo "Attempting to reset password via local psql..."
    
    # 尝试作为 postgres 用户执行 (需要 sudo 权限)
    if [ "$EUID" -ne 0 ]; then 
        echo "Please run with sudo to use local psql if needed."
        sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'postgres';"
    else
        su - postgres -c "psql -c \"ALTER USER postgres WITH PASSWORD 'postgres';\""
    fi
    
    if [ $? -eq 0 ]; then
        echo "✅ Password reset successfully via local psql!"
        exit 0
    fi
fi

echo "❌ Could not reset password. Please ensure PostgreSQL is running and you have permissions."
echo "Manual command: ALTER USER postgres WITH PASSWORD 'postgres';"
exit 1
