#!/usr/bin/env python3
"""
FHIR Converter Load Testing Framework
"""

import asyncio
import aiohttp
import json
import time
import statistics
import argparse
import sys
from datetime import datetime
from typing import Dict, List, Any
from dataclasses import dataclass

@dataclass
class TestResult:
    timestamp: float
    response_time: float
    status_code: int
    success: bool
    error_message: str = ""

@dataclass
class TestSummary:
    total_requests: int
    successful_requests: int
    failed_requests: int
    total_duration: float
    avg_response_time: float
    median_response_time: float
    p95_response_time: float
    p99_response_time: float
    min_response_time: float
    max_response_time: float
    requests_per_second: float
    error_rate: float

class FHIRConverterLoadTester:
    def __init__(self, config_file: str = "load-test-config.json"):
        self.config = self._load_config(config_file)
        self.results: List[TestResult] = []
        self.session: aiohttp.ClientSession = None
        
    def _load_config(self, config_file: str) -> Dict[str, Any]:
        try:
            with open(config_file, 'r') as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"Configuration file {config_file} not found!")
            sys.exit(1)
    
    async def __aenter__(self):
        timeout = aiohttp.ClientTimeout(total=30)
        connector = aiohttp.TCPConnector(limit=1000, limit_per_host=100)
        self.session = aiohttp.ClientSession(timeout=timeout, connector=connector)
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.session:
            await self.session.close()
    
    async def health_check(self) -> bool:
        try:
            url = f"{self.config['endpoints']['baseUrl']}{self.config['endpoints']['health']}"
            async with self.session.get(url) as response:
                return response.status == 200
        except Exception as e:
            print(f"Health check failed: {e}")
            return False
    
    async def send_request(self, endpoint: str, payload: str) -> TestResult:
        start_time = time.time()
        try:
            url = f"{self.config['endpoints']['baseUrl']}{endpoint}"
            async with self.session.post(url, data=payload, headers={'Content-Type': 'application/json'}) as response:
                response_time = (time.time() - start_time) * 1000
                response_text = await response.text()
                
                return TestResult(
                    timestamp=start_time,
                    response_time=response_time,
                    status_code=response.status,
                    success=response.status == 200,
                    error_message="" if response.status == 200 else response_text
                )
        except Exception as e:
            response_time = (time.time() - start_time) * 1000
            return TestResult(
                timestamp=start_time,
                response_time=response_time,
                status_code=0,
                success=False,
                error_message=str(e)
            )
    
    def _load_test_data(self, data_type: str) -> str:
        data_file = self.config['testData'].get(data_type)
        if not data_file:
            raise ValueError(f"No test data configured for {data_type}")
        
        try:
            with open(data_file, 'r') as f:
                data = f.read().strip()
            
            if data_type == "hl7v2":
                return json.dumps({
                    "inputDataFormat": "Hl7v2",
                    "inputDataString": data
                })
            elif data_type == "ccda":
                return json.dumps({
                    "inputDataFormat": "Ccda",
                    "inputDataString": data
                })
            elif data_type == "json":
                return json.dumps({
                    "inputDataFormat": "Json",
                    "inputDataString": data
                })
            else:
                raise ValueError(f"Unsupported data type: {data_type}")
        except FileNotFoundError:
            print(f"Test data file {data_file} not found!")
            sys.exit(1)
    
    async def run_scenario(self, scenario_name: str, data_type: str = "hl7v2") -> TestSummary:
        if scenario_name not in self.config['testScenarios']:
            raise ValueError(f"Unknown scenario: {scenario_name}")
        
        scenario = self.config['testScenarios'][scenario_name]
        print(f"\n🚀 Running {scenario_name} scenario: {scenario['description']}")
        print(f"   Requests: {scenario['requests']}, Concurrency: {scenario['concurrency']}")
        
        payload = self._load_test_data(data_type)
        endpoint = self.config['endpoints'][data_type]
        
        total_requests = scenario['requests']
        concurrency = scenario['concurrency']
        
        semaphore = asyncio.Semaphore(concurrency)
        
        async def make_request():
            async with semaphore:
                return await self.send_request(endpoint, payload)
        
        start_time = time.time()
        tasks = []
        
        for i in range(total_requests):
            task = asyncio.create_task(make_request())
            tasks.append(task)
        
        try:
            results = await asyncio.gather(*tasks)
            self.results.extend(results)
        except Exception as e:
            print(f"Test failed: {e}")
        
        end_time = time.time()
        total_duration = end_time - start_time
        
        return self._calculate_summary(total_duration)
    
    def _calculate_summary(self, total_duration: float) -> TestSummary:
        if not self.results:
            return TestSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        
        response_times = [r.response_time for r in self.results]
        successful_requests = sum(1 for r in self.results if r.success)
        failed_requests = len(self.results) - successful_requests
        
        return TestSummary(
            total_requests=len(self.results),
            successful_requests=successful_requests,
            failed_requests=failed_requests,
            total_duration=total_duration,
            avg_response_time=statistics.mean(response_times),
            median_response_time=statistics.median(response_times),
            p95_response_time=statistics.quantiles(response_times, n=20)[18],
            p99_response_time=statistics.quantiles(response_times, n=100)[98],
            min_response_time=min(response_times),
            max_response_time=max(response_times),
            requests_per_second=len(self.results) / total_duration,
            error_rate=failed_requests / len(self.results)
        )
    
    def print_summary(self, summary: TestSummary):
        print("\n📊 Test Results Summary:")
        print("=" * 50)
        print(f"Total Requests:     {summary.total_requests:,}")
        print(f"Successful:         {summary.successful_requests:,}")
        print(f"Failed:             {summary.failed_requests:,}")
        print(f"Error Rate:         {summary.error_rate:.2%}")
        print(f"Total Duration:     {summary.total_duration:.2f}s")
        print(f"Requests/Second:    {summary.requests_per_second:.2f}")
        print("\nResponse Times (ms):")
        print(f"  Average:          {summary.avg_response_time:.2f}")
        print(f"  Median:           {summary.median_response_time:.2f}")
        print(f"  95th Percentile:  {summary.p95_response_time:.2f}")
        print(f"  99th Percentile:  {summary.p99_response_time:.2f}")
        print(f"  Min:              {summary.min_response_time:.2f}")
        print(f"  Max:              {summary.max_response_time:.2f}")
        
        thresholds = self.config['performanceThresholds']
        print("\n🎯 Performance Assessment:")
        if (summary.avg_response_time <= thresholds['maxResponseTime'] and 
            summary.error_rate <= thresholds['maxErrorRate'] and 
            summary.requests_per_second >= thresholds['minThroughput']):
            print("✅ All thresholds met - Performance is GOOD")
        else:
            print("❌ Some thresholds exceeded - Performance needs improvement")
    
    def save_results(self, filename: str = None):
        if not filename:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"load-test-results_{timestamp}.json"
        
        results_data = {
            "timestamp": datetime.now().isoformat(),
            "config": self.config,
            "results": [
                {
                    "timestamp": r.timestamp,
                    "response_time": r.response_time,
                    "status_code": r.status_code,
                    "success": r.success,
                    "error_message": r.error_message
                }
                for r in self.results
            ]
        }
        
        with open(filename, 'w') as f:
            json.dump(results_data, f, indent=2)
        
        print(f"\n💾 Results saved to: {filename}")

async def main():
    parser = argparse.ArgumentParser(description='FHIR Converter Load Testing Framework')
    parser.add_argument('scenario', choices=['smoke', 'load', 'stress', 'spike', 'endurance', 'streaming'],
                       help='Test scenario to run')
    parser.add_argument('--data-type', choices=['hl7v2', 'ccda', 'json'], default='hl7v2',
                       help='Type of test data to use')
    parser.add_argument('--save-results', action='store_true',
                       help='Save detailed results to file')
    parser.add_argument('--config', default='load-test-config.json',
                       help='Configuration file path')
    
    args = parser.parse_args()
    
    print("🏥 FHIR Converter Load Testing Framework")
    print("=" * 50)
    
    async with FHIRConverterLoadTester(args.config) as tester:
        print("🔍 Performing health check...")
        if not await tester.health_check():
            print("❌ Health check failed! Make sure the API is running.")
            sys.exit(1)
        print("✅ Health check passed!")
        
        summary = await tester.run_scenario(args.scenario, args.data_type)
        tester.print_summary(summary)
        
        if args.save_results:
            tester.save_results()

if __name__ == "__main__":
    asyncio.run(main()) 