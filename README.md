# ai-chatbot
An AI-powered chat assistant integrated with a .NET backend and a React frontend. The chatbot will leverage Azure OpenAI for natural language processing and respond to user queries in real-time.

## Getting Started

Follow these steps to set up and run the project:

---

### 1. Set Up Infrastructure with Terraform

1. **Navigate to the Terraform Directory**:
  ```bash
  cd Terraform
  ```
2. **Initialize Terraform**:
  ```bash
  terraform init
  ```
3. **Apply the Terraform Configuration**:
  ```
  terraform apply
  ```
  * Review the plan and type yes to confirm.
  * This will create the necessary Azure resources, including the Cognitive Services account and App Service.

### 2. Build and Start the Backend API

1. **Navigate to the Backend Directory**:
   ```bash
   cd backend.api
   ```
2. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```
3. **Build the Project**:
   ```bash
   dotnet build
   ```
4. **Run the API**:
   ```bash
   dotnet run --launch-profile https
   ```
   * The API will start on [https://localhost:7256](https://localhost:7256/) (or another port if configured)..
   * Swagger UI is on [https://localhost:7256/swagger](https://localhost:7256/swagger).
