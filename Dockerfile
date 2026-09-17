FROM python:3.12-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
    nodejs \
    npm \
    git \
    ca-certificates \
    openssh-client \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g @anthropic-ai/claude-code

WORKDIR /workspace

COPY . .

ENTRYPOINT ["/workspace/entrypoint.sh"]
CMD ["claude"]