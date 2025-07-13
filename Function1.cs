using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp2
{
    internal class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("Ping")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Ping function executed.");

            var htmlContent = @"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Ping Response - Abhishek Tiwari</title>
                <style>
                    body {
                        margin: 0;
                        padding: 0;
                        height: 100vh;
                        background: linear-gradient(45deg, #1e3c72, #2a5298, #6dd5ed, #2193b0);
                        background-size: 400% 400%;
                        animation: gradientShift 8s ease infinite;
                        overflow: hidden;
                        font-family: 'Arial', sans-serif;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    }

                    @keyframes gradientShift {
                        0% { background-position: 0% 50%; }
                        50% { background-position: 100% 50%; }
                        100% { background-position: 0% 50%; }
                    }

                    .container {
                        position: relative;
                        width: 100%;
                        height: 100%;
                    }

                    .moving-text {
                        position: absolute;
                        font-size: 4rem;
                        font-weight: bold;
                        color: #fff;
                        text-shadow: 
                            0 0 10px rgba(255, 255, 255, 0.5),
                            0 0 20px rgba(255, 255, 255, 0.3),
                            0 0 30px rgba(255, 255, 255, 0.2);
                        white-space: nowrap;
                        animation: moveAcross 6s linear infinite;
                    }

                    @keyframes moveAcross {
                        0% {
                            transform: translateX(-100%);
                            opacity: 0;
                        }
                        10% {
                            opacity: 1;
                        }
                        90% {
                            opacity: 1;
                        }
                        100% {
                            transform: translateX(100vw);
                            opacity: 0;
                        }
                    }

                    .moving-text:nth-child(1) {
                        top: 20%;
                        animation-delay: 0s;
                        color: #ff6b6b;
                    }

                    .moving-text:nth-child(2) {
                        top: 40%;
                        animation-delay: 2s;
                        color: #4ecdc4;
                        animation-direction: reverse;
                    }

                    .moving-text:nth-child(3) {
                        top: 60%;
                        animation-delay: 4s;
                        color: #45b7d1;
                    }

                    .ping-status {
                        position: absolute;
                        top: 50%;
                        left: 50%;
                        transform: translate(-50%, -50%);
                        text-align: center;
                        z-index: 10;
                    }

                    .ping-status h1 {
                        font-size: 3rem;
                        color: #fff;
                        margin: 0;
                        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
                        animation: pulse 2s ease-in-out infinite;
                    }

                    .ping-status p {
                        font-size: 1.2rem;
                        color: #fff;
                        margin: 10px 0;
                        opacity: 0.8;
                    }

                    @keyframes pulse {
                        0% { transform: translate(-50%, -50%) scale(1); }
                        50% { transform: translate(-50%, -50%) scale(1.05); }
                        100% { transform: translate(-50%, -50%) scale(1); }
                    }

                    .particles {
                        position: absolute;
                        width: 100%;
                        height: 100%;
                        overflow: hidden;
                    }

                    .particle {
                        position: absolute;
                        width: 4px;
                        height: 4px;
                        background: #fff;
                        border-radius: 50%;
                        animation: float 6s infinite linear;
                    }

                    @keyframes float {
                        0% {
                            transform: translateY(100vh) rotate(0deg);
                            opacity: 0;
                        }
                        10% {
                            opacity: 1;
                        }
                        90% {
                            opacity: 1;
                        }
                        100% {
                            transform: translateY(-100vh) rotate(360deg);
                            opacity: 0;
                        }
                    }

                    .status-indicator {
                        position: absolute;
                        top: 20px;
                        right: 20px;
                        background: #4CAF50;
                        color: white;
                        padding: 10px 20px;
                        border-radius: 20px;
                        font-weight: bold;
                        animation: blink 1s infinite;
                    }

                    @keyframes blink {
                        0%, 50% { opacity: 1; }
                        51%, 100% { opacity: 0.7; }
                    }

                    @media (max-width: 768px) {
                        .moving-text {
                            font-size: 2rem;
                        }
                        .ping-status h1 {
                            font-size: 2rem;
                        }
                        .ping-status p {
                            font-size: 1rem;
                        }
                    }
                </style>
            </head>
            <body>
                <div class=""container"">
                    <div class=""particles""></div>
        
                    <div class=""moving-text"">abhishek tiwari</div>
                    <div class=""moving-text"">abhishek tiwari</div>
                    <div class=""moving-text"">abhishek tiwari</div>
        
                    <div class=""ping-status"">
                        <h1>🏓 PING SUCCESSFUL</h1>
                        <p>Server is alive and kicking!</p>
                        <p id=""timestamp""></p>
                    </div>
        
                    <div class=""status-indicator"">
                        ● ONLINE
                    </div>
                </div>

                <script>
                    // Update timestamp
                    document.getElementById('timestamp').textContent = new Date().toLocaleString();
        
                    // Create floating particles
                    function createParticles() {
                        const particles = document.querySelector('.particles');
                        for (let i = 0; i < 50; i++) {
                            const particle = document.createElement('div');
                            particle.className = 'particle';
                            particle.style.left = Math.random() * 100 + '%';
                            particle.style.animationDelay = Math.random() * 6 + 's';
                            particle.style.animationDuration = (Math.random() * 4 + 4) + 's';
                            particles.appendChild(particle);
                        }
                    }
        
                    createParticles();
        
                    // Add some interactivity
                    document.addEventListener('click', function(e) {
                        const ripple = document.createElement('div');
                        ripple.style.position = 'absolute';
                        ripple.style.left = e.clientX + 'px';
                        ripple.style.top = e.clientY + 'px';
                        ripple.style.width = '10px';
                        ripple.style.height = '10px';
                        ripple.style.background = 'rgba(255, 255, 255, 0.6)';
                        ripple.style.borderRadius = '50%';
                        ripple.style.transform = 'translate(-50%, -50%)';
                        ripple.style.animation = 'ripple 0.6s ease-out';
                        ripple.style.pointerEvents = 'none';
                        document.body.appendChild(ripple);
            
                        setTimeout(() => {
                            ripple.remove();
                        }, 600);
                    });
        
                    // Add ripple animation
                    const style = document.createElement('style');
                    style.textContent = `
                        @keyframes ripple {
                            0% {
                                transform: translate(-50%, -50%) scale(0);
                                opacity: 1;
                            }
                            100% {
                                transform: translate(-50%, -50%) scale(20);
                                opacity: 0;
                            }
                        }
                    `;
                    document.head.appendChild(style);
                </script>
            </body>
            </html>";

            return new ContentResult
            {
                Content = htmlContent,
                ContentType = "text/html",
                StatusCode = 200
            };
        }
    }
}
