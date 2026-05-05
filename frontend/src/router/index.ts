import { createRouter, createWebHistory } from 'vue-router'
import HomeView  from '../views/HomeView.vue'
import LobbyView from '../views/LobbyView.vue'
import GameView  from '../views/GameView.vue'

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/',      name: 'home',  component: HomeView  },
    { path: '/lobby', name: 'lobby', component: LobbyView },
    { path: '/game',  name: 'game',  component: GameView  },
  ]
})
