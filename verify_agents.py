import sys
import os

# Add the current directory to sys.path to make imports work
sys.path.append(os.getcwd())

from orchestrator import Orchestrator

def test_flow():
    print("Initializing Orchestrator...")
    try:
        orchestrator = Orchestrator()
    except Exception as e:
        print(f"FAILED to initialize Orchestrator: {e}")
        return

    test_input = "Create a simple calculator app"
    print(f"Running flow with input: '{test_input}'...")
    
    try:
        results = orchestrator.run_flow(test_input)
        
        print("\n--- Verifying Results ---")
        required_keys = ["requirements", "architecture", "implementation", "qa_report"]
        missing_keys = [key for key in required_keys if key not in results]
        
        if missing_keys:
            print(f"FAILED: Missing keys in result: {missing_keys}")
        else:
            print("SUCCESS: All keys present in result.")
            print("-" * 20)
            for key, value in results.items():
                print(f"{key.upper()}: {value[:50]}...")
            print("-" * 20)
            
    except Exception as e:
        print(f"FAILED during flow execution: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    test_flow()
