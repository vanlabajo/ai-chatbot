# ai-chatbot
An AI-powered chat assistant integrated with a .NET backend and a React frontend. The chatbot will leverage Azure OpenAI for natural language processing and respond to user queries in real-time.

## Getting Started

Follow these steps to set up and run the project:

---

### 1. Set Up Infrastructure with Terraform

1. **Navigate to the Terraform Directory**:
  ```bash
  cd Terraform
2. **Initialize Terraform**:
  ```
  terraform init
3. **Apply the Terraform Configuration**:
  ```
  terraform apply

- Review the plan and type yes to confirm.
- This will create the necessary Azure resources, including the Cognitive Services account and App Service.