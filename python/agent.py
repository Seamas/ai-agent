import tools
from pydantic_ai.models.openai import OpenAIChatModel
from pydantic_ai.providers.openai import OpenAIProvider
from pydantic_ai import Agent
from dotenv import load_dotenv
import os


load_dotenv()
api_key = os.getenv('DEEPSEEK_API_KEY')
provider = OpenAIProvider(
        base_url = "https://api.deepseek.com",
        api_key = api_key
    )
model = OpenAIChatModel('deepseek-chat', provider=provider)
agent = Agent(model, 
              system_prompt='你是一个资深的程序员',
              tools=[tools.list_files, tools.rename_file, tools.read_file])

def execute_agent():
    history = []
    while True:
        user_input = input("Input:")
        resp = agent.run_sync(user_input, message_history= history)
        history = list(resp.all_messages())
        print(resp)


if __name__ == "__main__":
    execute_agent()