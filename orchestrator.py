from agents import ProductOwnerAgent, TechArchitectAgent, DeveloperAgent, QAAgent
from utils.llm_client import LLMClient

class Orchestrator:
    def __init__(self):
        self.llm_client = LLMClient()
        self.po = ProductOwnerAgent(self.llm_client)
        self.architect = TechArchitectAgent(self.llm_client)
        self.developer = DeveloperAgent(self.llm_client)
        self.qa = QAAgent(self.llm_client)

    def run_flow(self, user_request: str):
        print("\n--- Starting Agent Flow ---\n")
        
        # Step 1: Product Owner
        print(">> Product Owner Working...")
        po_output = self.po.process(f"User Request: {user_request}")
        print(f"PO Output: {po_output}\n")

        # Step 2: Architect
        print(">> Tech Architect Working...")
        arch_output = self.architect.process(f"Requirements: {po_output}")
        print(f"Architect Output: {arch_output}\n")

        # Step 3: Developer
        print(">> Developer Working...")
        dev_output = self.developer.process(f"Architecture: {arch_output}")
        print(f"Developer Output: {dev_output}\n")

        # Step 4: QA
        print(">> QA Working...")
        qa_output = self.qa.process(f"Implementation: {dev_output}\nOriginal Requirements: {po_output}")
        print(f"QA Output: {qa_output}\n")

        }

    def run_agent(self, agent_name: str, user_input: str):
        print(f"\n--- Running Single Agent: {agent_name.upper()} ---\n")
        
        agent_map = {
            "po": self.po,
            "arch": self.architect,
            "dev": self.developer,
            "qa": self.qa
        }
        
        agent = agent_map.get(agent_name.lower())
        if not agent:
            return f"Error: Unknown agent '{agent_name}'. Available: {list(agent_map.keys())}"
            
        try:
            output = agent.process(user_input)
            print(f"{agent_name.upper()} Output: {output}\n")
            return output
        except Exception as e:
            return f"Error running agent {agent_name}: {e}"
