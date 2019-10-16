pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }
  }
  stages {
    stage('Build') {
      steps {
        bat 'nuget restore'
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU"'
        archiveArtifacts 'WpfKenBurns\\bin\\Release\\Ken Burns.scr'
      }
    }
  }
}
