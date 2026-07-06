FROM node:20-alpine

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

EXPOSE 3000

# Chạy Vite dev server và expose ra ngoài container
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
