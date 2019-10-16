pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }
  }
  stages {
    stage('') {
      steps {
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU"'
        archiveArtifacts 'WpfKenBurns\\bin\\Release\\Ken Burns.scr'
      }
    }
  }
}
