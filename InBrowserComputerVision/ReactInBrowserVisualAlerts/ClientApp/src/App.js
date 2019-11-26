import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { VisualAlerts } from './components/Home';


export default class App extends Component {
  displayName = App.name

  render() {
    return (
        <Layout>
            <Route exact path='/' component={VisualAlerts} />
        </Layout>
    );
  }
}
