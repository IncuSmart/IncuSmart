pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                bat 'dotnet restore IncuSmart.sln'
            }
        }

        stage('Build') {
            steps {
                bat 'dotnet build IncuSmart.sln -c Release --no-restore'
            }
        }

        stage('Publish') {
            steps {
                bat 'dotnet publish IncuSmart.App\\IncuSmart.App.csproj -c Release -o publish'
            }
        }

        stage('Deploy') {
            steps {
                bat '''
                set JENKINS_NODE_COOKIE=dontKillMe
                powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\\deploy_iis_and_ai.ps1
                '''
            }
        }
    }
}
