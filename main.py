from orchestrator import Orchestrator

import sys
import argparse
from orchestrator import Orchestrator

def main():
    parser = argparse.ArgumentParser(description="Multi-Agent System")
    parser.add_argument("prompt", nargs="?", help="The project idea or request")
    parser.add_argument("--agent", help="Specific agent to run (po, arch, dev, qa)")
    
    args = parser.parse_args()

    orchestrator = Orchestrator()
    
    if args.agent:
        if not args.prompt:
            print(f"Error: Prompt is required for agent {args.agent}")
            return
        result = orchestrator.run_agent(args.agent, args.prompt)
    else:
        user_input = args.prompt
        if not user_input:
            print("Welcome to the Multi-Agent System Setup!")
            print("This system has 4 agents: Product Owner, Architect, Developer, QA.")
            user_input = input("Enter a project idea (or hit Enter for a default 'Snake Game'): ").strip()
            
        if not user_input:
            user_input = "Create a simple command-line Snake Game."
            
        results = orchestrator.run_flow(user_input)
        print("\n--- Final Results ---")
        print("Flow completed successfully.")

if __name__ == "__main__":
    main()
